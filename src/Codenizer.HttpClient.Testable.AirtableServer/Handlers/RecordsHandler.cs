using Codenizer.HttpClient.Testable.AirtableServer.Models;
using Newtonsoft.Json.Linq;

namespace Codenizer.HttpClient.Testable.AirtableServer.Handlers
{
    /// <summary>
    /// Handles Airtable Records API endpoints.
    /// </summary>
    public class RecordsHandler
    {
        private readonly AirtableState _state;

        public RecordsHandler(AirtableState state)
        {
            _state = state;
        }

        /// <summary>
        /// List records from a table.
        /// GET /v0/{baseId}/{tableIdOrName}
        /// 
        /// Supported query parameters:
        /// - pageSize: Number of records per page (max 100)
        /// - offset: Pagination cursor
        /// - fields[]: Specific fields to return
        /// - filterByFormula: Formula to filter records
        /// - maxRecords: Maximum total records to return
        /// - sort[0][field], sort[0][direction]: Sort configuration
        /// - view: View ID or name to use for filtering/sorting
        /// - cellFormat: "json" or "string"
        /// - timeZone: Timezone for date formatting
        /// - userLocale: Locale for formatting
        /// - returnFieldsByFieldId: Return field IDs instead of names
        /// </summary>
        public object List(string baseId, string tableIdOrName, Dictionary<string, string> queryParams)
        {
            var table = _state.GetTable(baseId, tableIdOrName);
            if (table == null)
            {
                return new { error = new { type = "TABLE_NOT_FOUND", message = $"Table '{tableIdOrName}' not found" } };
            }

            // Get all records first
            var allRecords = _state.GetRecords(baseId, tableIdOrName);
            
            // Apply View settings if 'view' parameter is present
            if (queryParams.TryGetValue("view", out var viewIdOrName))
            {
                var view = table.Views.FirstOrDefault(v => 
                    v.Id == viewIdOrName || 
                    v.Name.Equals(viewIdOrName, StringComparison.OrdinalIgnoreCase));

                if (view != null)
                {
                    // Apply view sorting if request doesn't override it
                    if (!queryParams.Keys.Any(k => k.StartsWith("sort[")) && view.Sort != null)
                    {
                        var sortConfigs = view.Sort.Select(s => (s.Field, s.Direction == "desc")).ToList();
                        allRecords = ApplySorting(allRecords, sortConfigs);
                    }
                    
                    // Apply view filtering if request doesn't override it
                    if (!queryParams.ContainsKey("filterByFormula") && !string.IsNullOrEmpty(view.FilterFormula))
                    {
                        allRecords = ApplyFormula(allRecords, view.FilterFormula);
                    }
                }
            }

            // Apply filterByFormula (simplified - only supports basic equality)
            if (queryParams.TryGetValue("filterByFormula", out var formula) && !string.IsNullOrEmpty(formula))
            {
                allRecords = ApplyFormula(allRecords, formula);
            }

            // Apply explicit sorting from query params
            if (queryParams.Keys.Any(k => k.StartsWith("sort[")))
            {
                allRecords = ApplySorting(allRecords, queryParams);
            }

            // Apply maxRecords limit
            if (queryParams.TryGetValue("maxRecords", out var maxStr) && int.TryParse(maxStr, out var maxRecords))
            {
                allRecords = allRecords.Take(maxRecords).ToList();
            }

            // Pagination
            int pageSize = 100;
            if (queryParams.TryGetValue("pageSize", out var ps) && int.TryParse(ps, out var psVal))
            {
                pageSize = Math.Clamp(psVal, 1, 100);
            }

            string? offset = queryParams.TryGetValue("offset", out var off) ? off : null;
            int startIndex = 0;
            if (offset != null && int.TryParse(offset, out var offsetInt))
            {
                startIndex = offsetInt;
            }

            var paginatedRecords = allRecords.Skip(startIndex).Take(pageSize).ToList();
            var hasMore = allRecords.Count > startIndex + pageSize;

            // Apply field filtering
            var fieldsToInclude = GetFieldsToInclude(queryParams);
            
            // If fields not specified in params, check view
            if (fieldsToInclude.Count == 0 && queryParams.TryGetValue("view", out viewIdOrName))
            {
                 var view = table.Views.FirstOrDefault(v => 
                    v.Id == viewIdOrName || 
                    v.Name.Equals(viewIdOrName, StringComparison.OrdinalIgnoreCase));
                 
                 if (view?.VisibleFields != null)
                 {
                     fieldsToInclude = view.VisibleFields;
                 }
            }

            var filteredRecords = FilterRecordFields(paginatedRecords, fieldsToInclude);

            var result = new Dictionary<string, object>
            {
                { "records", filteredRecords }
            };

            if (hasMore)
            {
                result["offset"] = (startIndex + pageSize).ToString();
            }

            return result;
        }

        /// <summary>
        /// Get a single record.
        /// GET /v0/{baseId}/{tableIdOrName}/{recordId}
        /// </summary>
        public object Get(string baseId, string tableIdOrName, string recordId, Dictionary<string, string>? queryParams = null)
        {
            var table = _state.GetTable(baseId, tableIdOrName);
            if (table == null)
            {
                return new { error = new { type = "TABLE_NOT_FOUND", message = $"Table '{tableIdOrName}' not found" } };
            }

            var record = _state.GetRecord(baseId, tableIdOrName, recordId);
            if (record == null)
            {
                return new { error = new { type = "NOT_FOUND", message = $"Record '{recordId}' not found" } };
            }

            // Apply field filtering if specified
            if (queryParams != null)
            {
                var fieldsToInclude = GetFieldsToInclude(queryParams);
                if (fieldsToInclude.Count > 0)
                {
                    return FilterRecordFields(new List<AirtableRecord> { record }, fieldsToInclude).First();
                }
            }

            return record;
        }

        /// <summary>
        /// Create records in a table.
        /// POST /v0/{baseId}/{tableIdOrName}
        /// </summary>
        public object Create(string baseId, string tableIdOrName, JObject body)
        {
            var table = _state.GetTable(baseId, tableIdOrName);
            if (table == null)
            {
                return new { error = new { type = "TABLE_NOT_FOUND", message = $"Table '{tableIdOrName}' not found" } };
            }

            var createdRecords = new List<AirtableRecord>();
            var typecast = body["typecast"]?.Value<bool>() ?? false;

            // Handle single record with "fields" key
            if (body.TryGetValue("fields", out var fieldsToken))
            {
                var fields = ParseFields(fieldsToken, typecast);
                ProcessFields(table, fields);
                ValidateFields(table, fields, typecast);
                var record = _state.CreateRecord(baseId, tableIdOrName, fields);
                createdRecords.Add(record);
            }
            // Handle multiple records with "records" array
            else if (body.TryGetValue("records", out var recordsToken) && recordsToken is JArray recordsArray)
            {
                if (recordsArray.Count > 10)
                {
                    return new { error = new { type = "INVALID_REQUEST_UNKNOWN", message = "Cannot create more than 10 records at once" } };
                }

                foreach (var recordToken in recordsArray)
                {
                    var recordObj = recordToken as JObject;
                    var fields = ParseFields(recordObj?["fields"], typecast);
                    ProcessFields(table, fields);
                    ValidateFields(table, fields, typecast);
                    var record = _state.CreateRecord(baseId, tableIdOrName, fields);
                    createdRecords.Add(record);
                }
            }
            else
            {
                return new { error = new { type = "INVALID_REQUEST_UNKNOWN", message = "Request must include 'fields' or 'records'" } };
            }

            return new { records = createdRecords };
        }

        /// <summary>
        /// Update multiple records.
        /// PATCH/PUT /v0/{baseId}/{tableIdOrName}
        /// </summary>
        public object UpdateMultiple(string baseId, string tableIdOrName, JObject body, bool destructive = false)
        {
            var table = _state.GetTable(baseId, tableIdOrName);
            if (table == null)
            {
                return new { error = new { type = "TABLE_NOT_FOUND", message = $"Table '{tableIdOrName}' not found" } };
            }

            var typecast = body["typecast"]?.Value<bool>() ?? false;
            var performUpsert = body["performUpsert"]?.Value<bool>() ?? false;
            var fieldsToMergeOn = body["fieldsToMergeOn"]?.ToObject<List<string>>();

            if (!body.TryGetValue("records", out var recordsToken) || recordsToken is not JArray recordsArray)
            {
                return new { error = new { type = "INVALID_REQUEST_UNKNOWN", message = "Request must include 'records' array" } };
            }

            if (recordsArray.Count > 10)
            {
                return new { error = new { type = "INVALID_REQUEST_UNKNOWN", message = "Cannot update more than 10 records at once" } };
            }

            var updatedRecords = new List<AirtableRecord>();
            var createdRecords = new List<string>();
            var updatedIds = new List<string>();

            foreach (var recordToken in recordsArray)
            {
                var recordObj = recordToken as JObject;
                var recordId = recordObj?["id"]?.ToString();
                var fields = ParseFields(recordObj?["fields"], typecast);

                // Handle upsert
                if (performUpsert && fieldsToMergeOn != null && fieldsToMergeOn.Count > 0)
                {
                    var existingRecord = FindRecordByMergeFields(baseId, tableIdOrName, fields, fieldsToMergeOn);
                    if (existingRecord != null)
                    {
                        recordId = existingRecord.Id;
                    }
                    else
                    {
                        // Create new record
                        ProcessFields(table, fields);
                        ValidateFields(table, fields, typecast);
                        var newRecord = _state.CreateRecord(baseId, tableIdOrName, fields);
                        updatedRecords.Add(newRecord);
                        createdRecords.Add(newRecord.Id);
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(recordId))
                {
                    return new { error = new { type = "INVALID_REQUEST_UNKNOWN", message = "Each record must have an 'id'" } };
                }

                ProcessFields(table, fields);
                ValidateFields(table, fields, typecast);
                var updatedRecord = _state.UpdateRecord(baseId, tableIdOrName, recordId, fields, destructive);
                if (updatedRecord == null)
                {
                    return new { error = new { type = "NOT_FOUND", message = $"Record '{recordId}' not found" } };
                }

                updatedRecords.Add(updatedRecord);
                updatedIds.Add(recordId);
            }

            if (performUpsert)
            {
                return new
                {
                    records = updatedRecords,
                    createdRecords = createdRecords,
                    updatedRecords = updatedIds
                };
            }

            return new { records = updatedRecords };
        }

        /// <summary>
        /// Update a single record.
        /// PATCH/PUT /v0/{baseId}/{tableIdOrName}/{recordId}
        /// </summary>
        public object UpdateSingle(string baseId, string tableIdOrName, string recordId, JObject body, bool destructive = false)
        {
            var table = _state.GetTable(baseId, tableIdOrName);
            if (table == null)
            {
                return new { error = new { type = "TABLE_NOT_FOUND", message = $"Table '{tableIdOrName}' not found" } };
            }

            var typecast = body["typecast"]?.Value<bool>() ?? false;
            var fields = ParseFields(body["fields"], typecast);
            ProcessFields(table, fields);
            ValidateFields(table, fields, typecast);
            
            var updatedRecord = _state.UpdateRecord(baseId, tableIdOrName, recordId, fields, destructive);

            if (updatedRecord == null)
            {
                return new { error = new { type = "NOT_FOUND", message = $"Record '{recordId}' not found" } };
            }

            return updatedRecord;
        }

        /// <summary>
        /// Delete multiple records.
        /// DELETE /v0/{baseId}/{tableIdOrName}?records[]={id1}&records[]={id2}
        /// </summary>
        public object DeleteMultiple(string baseId, string tableIdOrName, List<string> recordIds)
        {
            var table = _state.GetTable(baseId, tableIdOrName);
            if (table == null)
            {
                return new { error = new { type = "TABLE_NOT_FOUND", message = $"Table '{tableIdOrName}' not found" } };
            }

            if (recordIds.Count > 10)
            {
                return new { error = new { type = "INVALID_REQUEST_UNKNOWN", message = "Cannot delete more than 10 records at once" } };
            }

            var deletedRecords = new List<object>();

            foreach (var recordId in recordIds)
            {
                var deleted = _state.DeleteRecord(baseId, tableIdOrName, recordId);
                deletedRecords.Add(new { id = recordId, deleted });
            }

            return new { records = deletedRecords };
        }

        /// <summary>
        /// Delete a single record.
        /// DELETE /v0/{baseId}/{tableIdOrName}/{recordId}
        /// </summary>
        public object DeleteSingle(string baseId, string tableIdOrName, string recordId)
        {
            var table = _state.GetTable(baseId, tableIdOrName);
            if (table == null)
            {
                return new { error = new { type = "TABLE_NOT_FOUND", message = $"Table '{tableIdOrName}' not found" } };
            }

            var deleted = _state.DeleteRecord(baseId, tableIdOrName, recordId);

            return new { id = recordId, deleted };
        }

        #region Private Helpers

        private void ValidateFields(AirtableTable table, Dictionary<string, object?> fields, bool typecast)
        {
            foreach (var kvp in fields)
            {
                var fieldName = kvp.Key;
                var value = kvp.Value;
                
                var field = table.Fields.FirstOrDefault(f => f.Name == fieldName || f.Id == fieldName);
                if (field == null)
                {
                    // Airtable ignores unknown fields, so we won't throw exception, just continue
                    continue;
                }

                // Basic type validation
                switch (field.Type)
                {
                    case AirtableFieldTypes.Number:
                    case AirtableFieldTypes.Currency:
                    case AirtableFieldTypes.Percent:
                        if (value != null && !(value is long || value is int || value is double || value is float))
                        {
                            if (!typecast)
                                throw new ArgumentException($"Field '{fieldName}' expects a number");
                            // If typecast is true, we assume it's converted or allow it
                        }
                        break;

                    case AirtableFieldTypes.Checkbox:
                        if (value != null && !(value is bool))
                        {
                             if (!typecast)
                                throw new ArgumentException($"Field '{fieldName}' expects a boolean");
                        }
                        break;
                        
                    case AirtableFieldTypes.SingleSelect:
                        if (value != null && value is string optionName)
                        {
                            // In a real implementation we would check options, but for now we just ensure it's a string
                        }
                        break;
                        
                    case AirtableFieldTypes.LinkedRecord:
                        if (value != null)
                        {
                            var linkedRecords = value as JArray;
                            if (linkedRecords == null && value is List<object> list)
                            {
                                linkedRecords = JArray.FromObject(list);
                            }

                            if (linkedRecords != null)
                            {
                                var linkedTableId = field.Options?["linkedTableId"]?.ToString();
                                
                                // Ideally we validate against the linked table
                                if (!string.IsNullOrEmpty(linkedTableId))
                                {
                                    // We need to find the base that contains this table.
                                    // Since we don't have baseId easily available here without passing it down,
                                    // we might skip strict validation or we assume it's in the same base for now.
                                    // BUT, we have _state.
                                    
                                    // For simulation simplicity, we'll check if we can find the table using the linkedTableId
                                    // iterating over all bases is expensive but safe for simulation
                                    var targetTable = _state.Tables.Values
                                        .SelectMany(baseTables => baseTables.Values)
                                        .FirstOrDefault(t => t.Id == linkedTableId);

                                    if (targetTable != null)
                                    {
                                        foreach (var link in linkedRecords)
                                        {
                                            string? idToCheck = null;
                                            
                                            if (link.Type == JTokenType.String)
                                                idToCheck = link.Value<string>();
                                            else if (link.Type == JTokenType.Object) // Handle objects like { "id": "rec..." }
                                                idToCheck = link["id"]?.Value<string>();
                                                
                                            if (!string.IsNullOrEmpty(idToCheck))
                                            {
                                                 // Verify record exists
                                                 // We need to know which base this table belongs to to use GetRecord efficiently
                                                 // But we found the table object, we can't easily find records without baseId and tableId
                                                 // Let's rely on finding the record globally or searching
                                                 
                                                 var recordExists = _state.Records.Values // base level
                                                     .Any(baseRecords => baseRecords.TryGetValue(targetTable.Name, out var tableRecords) && tableRecords.ContainsKey(idToCheck));
                                                     
                                                 if (!recordExists) 
                                                 {
                                                     // Also check by ID if table name didn't match (though key is usually table name or ID)
                                                     recordExists = _state.Records.Values
                                                        .Any(baseRecords => baseRecords.TryGetValue(targetTable.Id, out var tableRecords) && tableRecords.ContainsKey(idToCheck));
                                                 }

                                                 if (!recordExists)
                                                 {
                                                     throw new ArgumentException($"Linked record '{idToCheck}' does not exist in table '{targetTable.Name}'");
                                                 }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                }
            }
        }

        private void ProcessFields(AirtableTable table, Dictionary<string, object?> fields)
        {
            foreach (var key in fields.Keys.ToList())
            {
                var fieldName = key;
                var value = fields[key];
                
                var field = table.Fields.FirstOrDefault(f => f.Name == fieldName || f.Id == fieldName);
                if (field == null) continue;

                if (field.Type == AirtableFieldTypes.Attachment && value != null)
                {
                    // Enrich attachment data
                    var attachments = value as List<object>;
                    if (attachments == null && value is JArray jArray)
                    {
                        attachments = jArray.ToObject<List<object>>();
                    }
                    
                    if (attachments != null)
                    {
                        var processedAttachments = new List<object>();
                        foreach (var att in attachments)
                        {
                            if (att is JObject attObj)
                            {
                                // Simulate processing
                                if (attObj["id"] == null) attObj["id"] = $"att{Guid.NewGuid().ToString("N").Substring(0, 14)}";
                                if (attObj["filename"] == null) attObj["filename"] = Path.GetFileName(attObj["url"]?.ToString()) ?? "file.png";
                                if (attObj["size"] == null) attObj["size"] = 1024; // Mock size
                                if (attObj["type"] == null) attObj["type"] = "image/png"; // Mock type
                                if (attObj["thumbnails"] == null) 
                                {
                                    attObj["thumbnails"] = JObject.FromObject(new { 
                                        small = new { url = "https://example.com/small.png", width = 36, height = 36 },
                                        large = new { url = "https://example.com/large.png", width = 512, height = 512 }
                                    });
                                }
                                processedAttachments.Add(attObj);
                            }
                            else if (att is Dictionary<string, object> attDict)
                            {
                                // Handle dictionary
                                if (!attDict.ContainsKey("id")) attDict["id"] = $"att{Guid.NewGuid().ToString("N").Substring(0, 14)}";
                                if (!attDict.ContainsKey("filename")) attDict["filename"] = Path.GetFileName(attDict.GetValueOrDefault("url")?.ToString()) ?? "file.png";
                                if (!attDict.ContainsKey("size")) attDict["size"] = 1024;
                                if (!attDict.ContainsKey("type")) attDict["type"] = "image/png";
                                processedAttachments.Add(attDict);
                            }
                            else
                            {
                                processedAttachments.Add(att);
                            }
                        }
                        fields[key] = processedAttachments;
                    }
                }
            }
        }

        private Dictionary<string, object?> ParseFields(JToken? fieldsToken, bool typecast)
        {
            if (fieldsToken == null)
                return new Dictionary<string, object?>();

            var fields = new Dictionary<string, object?>();
            
            if (fieldsToken is JObject fieldsObj)
            {
                foreach (var prop in fieldsObj.Properties())
                {
                    fields[prop.Name] = ConvertJTokenToValue(prop.Value, typecast);
                }
            }

            return fields;
        }

        private object? ConvertJTokenToValue(JToken token, bool typecast)
        {
            return token.Type switch
            {
                JTokenType.String => token.Value<string>(),
                JTokenType.Integer => token.Value<long>(),
                JTokenType.Float => token.Value<double>(),
                JTokenType.Boolean => token.Value<bool>(),
                JTokenType.Null => null,
                JTokenType.Array => token.ToObject<List<object>>(),
                JTokenType.Object => token.ToObject<Dictionary<string, object>>(),
                _ => token.ToString()
            };
        }

        private List<string> GetFieldsToInclude(Dictionary<string, string> queryParams)
        {
            var fields = new List<string>();
            
            // Handle fields[] array notation
            foreach (var key in queryParams.Keys)
            {
                if (key == "fields[]" || key.StartsWith("fields["))
                {
                    fields.Add(queryParams[key]);
                }
            }

            // Also check for comma-separated fields parameter
            if (queryParams.TryGetValue("fields", out var fieldsValue))
            {
                fields.AddRange(fieldsValue.Split(',', StringSplitOptions.RemoveEmptyEntries));
            }

            return fields;
        }

        private List<AirtableRecord> FilterRecordFields(List<AirtableRecord> records, List<string> fieldsToInclude)
        {
            if (fieldsToInclude.Count == 0)
                return records;

            return records.Select(r => new AirtableRecord
            {
                Id = r.Id,
                CreatedTime = r.CreatedTime,
                Fields = r.Fields
                    .Where(kvp => fieldsToInclude.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            }).ToList();
        }

        private List<AirtableRecord> ApplyFormula(List<AirtableRecord> records, string formula)
        {
            formula = formula.Trim();
            
            // Handle AND(...)
            if (formula.StartsWith("AND(", StringComparison.OrdinalIgnoreCase) && formula.EndsWith(")"))
            {
                var content = formula.Substring(4, formula.Length - 5);
                var parts = SplitFormulaArguments(content);
                
                var result = records;
                foreach (var part in parts)
                {
                    result = ApplyFormula(result, part);
                }
                return result;
            }

            // Handle OR(...)
            if (formula.StartsWith("OR(", StringComparison.OrdinalIgnoreCase) && formula.EndsWith(")"))
            {
                var content = formula.Substring(3, formula.Length - 4);
                var parts = SplitFormulaArguments(content);
                
                var result = new HashSet<AirtableRecord>();
                foreach (var part in parts)
                {
                    var partialResult = ApplyFormula(records, part);
                    foreach(var r in partialResult) result.Add(r);
                }
                // Sort by ID to maintain deterministic order usually
                return result.OrderBy(r => r.Id).ToList();
            }

            // Handle NOT(...)
            if (formula.StartsWith("NOT(", StringComparison.OrdinalIgnoreCase) && formula.EndsWith(")"))
            {
                var content = formula.Substring(4, formula.Length - 5);
                // Inverse logic: Find records matching inner formula, then exclude them
                var matchingInner = ApplyFormula(records, content).Select(r => r.Id).ToHashSet();
                return records.Where(r => !matchingInner.Contains(r.Id)).ToList();
            }

            // Handle FIND('substring', {FieldName})
            // Matches: FIND('val', {Field}) or FIND("val", {Field})
            var findMatch = System.Text.RegularExpressions.Regex.Match(
                formula,
                @"FIND\(['""]([^'""]*)['""],\s*\{([^}]+)\}\)", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (findMatch.Success)
            {
                var substring = findMatch.Groups[1].Value;
                var fieldName = findMatch.Groups[2].Value;

                return records.Where(r =>
                {
                    if (r.Fields.TryGetValue(fieldName, out var actualValue))
                    {
                        return actualValue?.ToString()?.Contains(substring, StringComparison.OrdinalIgnoreCase) ?? false;
                    }
                    return false;
                }).ToList();
            }

            // Handle simple equality: {FieldName} = 'value' or "value"
            var equalityMatch = System.Text.RegularExpressions.Regex.Match(
                formula, 
                @"\{([^}]+)\}\s*=\s*['""]([^'""]*)['""]");
            
            if (equalityMatch.Success)
            {
                var fieldName = equalityMatch.Groups[1].Value;
                var expectedValue = equalityMatch.Groups[2].Value;
                
                return records.Where(r =>
                {
                    if (r.Fields.TryGetValue(fieldName, out var actualValue))
                    {
                        return string.Equals(actualValue?.ToString(), expectedValue, StringComparison.OrdinalIgnoreCase);
                    }
                    return false;
                }).ToList();
            }

            // If formula not recognized or empty, return all records
            return records;
        }

        private List<string> SplitFormulaArguments(string content)
        {
            var parts = new List<string>();
            var currentPart = new System.Text.StringBuilder();
            int parenthesesLevel = 0;
            bool insideQuote = false;
            char quoteChar = '\0';

            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];

                if (insideQuote)
                {
                    currentPart.Append(c);
                    if (c == quoteChar) insideQuote = false;
                }
                else
                {
                    if (c == '\'' || c == '"')
                    {
                        insideQuote = true;
                        quoteChar = c;
                        currentPart.Append(c);
                    }
                    else if (c == '(')
                    {
                        parenthesesLevel++;
                        currentPart.Append(c);
                    }
                    else if (c == ')')
                    {
                        parenthesesLevel--;
                        currentPart.Append(c);
                    }
                    else if (c == ',' && parenthesesLevel == 0)
                    {
                        parts.Add(currentPart.ToString().Trim());
                        currentPart.Clear();
                    }
                    else
                    {
                        currentPart.Append(c);
                    }
                }
            }

            if (currentPart.Length > 0)
                parts.Add(currentPart.ToString().Trim());

            return parts;
        }

        private List<AirtableRecord> ApplySorting(List<AirtableRecord> records, Dictionary<string, string> queryParams)
        {
            // Parse sort parameters: sort[0][field], sort[0][direction]
            var sortConfigs = new List<(string field, bool descending)>();

            for (int i = 0; i < 10; i++)
            {
                var fieldKey = $"sort[{i}][field]";
                var directionKey = $"sort[{i}][direction]";

                if (queryParams.TryGetValue(fieldKey, out var field))
                {
                    var descending = queryParams.TryGetValue(directionKey, out var dir) 
                        && dir.Equals("desc", StringComparison.OrdinalIgnoreCase);
                    sortConfigs.Add((field, descending));
                }
                else
                {
                    break;
                }
            }

            return ApplySorting(records, sortConfigs);
        }

        private List<AirtableRecord> ApplySorting(List<AirtableRecord> records, List<(string field, bool descending)> sortConfigs)
        {
            if (sortConfigs.Count == 0)
                return records;

            // Apply sorting in reverse order (last sort is primary)
            IEnumerable<AirtableRecord> sorted = records;
            
            for (int i = sortConfigs.Count - 1; i >= 0; i--)
            {
                var (field, descending) = sortConfigs[i];
                
                if (i == sortConfigs.Count - 1)
                {
                    sorted = descending
                        ? sorted.OrderByDescending(r => GetFieldValue(r, field))
                        : sorted.OrderBy(r => GetFieldValue(r, field));
                }
                else
                {
                    var orderedSorted = sorted as IOrderedEnumerable<AirtableRecord>;
                    if (orderedSorted != null)
                    {
                        sorted = descending
                            ? orderedSorted.ThenByDescending(r => GetFieldValue(r, field))
                            : orderedSorted.ThenBy(r => GetFieldValue(r, field));
                    }
                }
            }

            return sorted.ToList();
        }

        private object? GetFieldValue(AirtableRecord record, string fieldName)
        {
            return record.Fields.TryGetValue(fieldName, out var value) ? value : null;
        }

        private AirtableRecord? FindRecordByMergeFields(string baseId, string tableIdOrName, Dictionary<string, object?> fields, List<string> fieldsToMergeOn)
        {
            var allRecords = _state.GetRecords(baseId, tableIdOrName);
            
            return allRecords.FirstOrDefault(r =>
                fieldsToMergeOn.All(mergeField =>
                {
                    if (!fields.TryGetValue(mergeField, out var newValue))
                        return false;
                    if (!r.Fields.TryGetValue(mergeField, out var existingValue))
                        return false;
                    
                    return string.Equals(newValue?.ToString(), existingValue?.ToString(), StringComparison.OrdinalIgnoreCase);
                })
            );
        }

        #endregion
    }
}
