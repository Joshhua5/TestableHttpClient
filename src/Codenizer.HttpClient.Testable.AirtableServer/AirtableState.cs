using Codenizer.HttpClient.Testable.AirtableServer.Models;
using Newtonsoft.Json.Linq;

namespace Codenizer.HttpClient.Testable.AirtableServer
{
    /// <summary>
    /// Manages the stateful data for the Airtable emulator.
    /// </summary>
    public class AirtableState
    {
        private int _baseCounter = 1;
        private int _tableCounter = 1;
        private int _fieldCounter = 1;
        private int _viewCounter = 1;
        private int _recordCounter = 1;
        private int _commentCounter = 1;
        private int _webhookCounter = 1;
        private int _userCounter = 1;
        private int _attachmentCounter = 1;

        /// <summary>
        /// Dictionary of bases keyed by base ID.
        /// </summary>
        public Dictionary<string, AirtableBase> Bases { get; } = new();

        /// <summary>
        /// Dictionary of tables keyed by table ID, organized by base ID.
        /// </summary>
        public Dictionary<string, Dictionary<string, AirtableTable>> Tables { get; } = new();

        /// <summary>
        /// Dictionary of records keyed by record ID, organized by base ID and table ID.
        /// </summary>
        public Dictionary<string, Dictionary<string, Dictionary<string, AirtableRecord>>> Records { get; } = new();

        /// <summary>
        /// Dictionary of comments keyed by comment ID, organized by record key (baseId:tableId:recordId).
        /// </summary>
        public Dictionary<string, Dictionary<string, AirtableComment>> Comments { get; } = new();

        /// <summary>
        /// Dictionary of webhooks keyed by webhook ID, organized by base ID.
        /// </summary>
        public Dictionary<string, Dictionary<string, AirtableWebhook>> Webhooks { get; } = new();

        /// <summary>
        /// Dictionary of webhook payloads keyed by webhook ID.
        /// </summary>
        public Dictionary<string, List<AirtableWebhookPayload>> WebhookPayloads { get; } = new();

        /// <summary>
        /// The current authenticated user.
        /// </summary>
        public AirtableUser CurrentUser { get; set; } = new()
        {
            Id = "usr00000000000001",
            Email = "testuser@example.com",
            Scopes = new AirtableTokenScopes
            {
                AccountScopes = new List<string> { "account.info:read" },
                BaseScopesInfo = new AirtableBaseScopesInfo
                {
                    Permissions = new List<string>
                    {
                        "data.records:read",
                        "data.records:write",
                        "schema.bases:read",
                        "schema.bases:write",
                        "webhook:manage"
                    }
                },
                UserTokenScopes = new List<string> { "user.email:read" }
            }
        };

        public AirtableState()
        {
            SeedDefaultData();
        }

        private void SeedDefaultData()
        {
            // Create a default base
            var defaultBase = new AirtableBase
            {
                Id = "app00000000000001",
                Name = "Test Base",
                PermissionLevel = "create"
            };
            Bases[defaultBase.Id] = defaultBase;
            Tables[defaultBase.Id] = new Dictionary<string, AirtableTable>();
            Records[defaultBase.Id] = new Dictionary<string, Dictionary<string, AirtableRecord>>();
            Webhooks[defaultBase.Id] = new Dictionary<string, AirtableWebhook>();

            // Create a default table with fields
            var nameField = new AirtableField
            {
                Id = "fld00000000000001",
                Name = "Name",
                Type = AirtableFieldTypes.SingleLineText
            };

            var statusField = new AirtableField
            {
                Id = "fld00000000000002",
                Name = "Status",
                Type = AirtableFieldTypes.SingleSelect
            };

            var notesField = new AirtableField
            {
                Id = "fld00000000000003",
                Name = "Notes",
                Type = AirtableFieldTypes.MultilineText
            };

            var createdTimeField = new AirtableField
            {
                Id = "fld00000000000004",
                Name = "Created",
                Type = AirtableFieldTypes.CreatedTime
            };

            var defaultView = new AirtableView
            {
                Id = "viw00000000000001",
                Name = "Grid view",
                Type = AirtableViewTypes.Grid
            };

            var defaultTable = new AirtableTable
            {
                Id = "tbl00000000000001",
                Name = "Tasks",
                Description = "A table for tracking tasks",
                PrimaryFieldId = nameField.Id,
                Fields = new List<AirtableField> { nameField, statusField, notesField, createdTimeField },
                Views = new List<AirtableView> { defaultView }
            };

            Tables[defaultBase.Id][defaultTable.Id] = defaultTable;
            Records[defaultBase.Id][defaultTable.Id] = new Dictionary<string, AirtableRecord>();

            // Add some sample records
            var record1 = new AirtableRecord
            {
                Id = "rec00000000000001",
                CreatedTime = DateTime.UtcNow.AddDays(-1).ToString("o"),
                Fields = new Dictionary<string, object?>
                {
                    { "Name", "First Task" },
                    { "Status", "In Progress" },
                    { "Notes", "This is the first task" }
                }
            };

            var record2 = new AirtableRecord
            {
                Id = "rec00000000000002",
                CreatedTime = DateTime.UtcNow.ToString("o"),
                Fields = new Dictionary<string, object?>
                {
                    { "Name", "Second Task" },
                    { "Status", "Todo" },
                    { "Notes", "This is the second task" }
                }
            };

            Records[defaultBase.Id][defaultTable.Id][record1.Id] = record1;
            Records[defaultBase.Id][defaultTable.Id][record2.Id] = record2;

            // Create a second table
            var contactsTable = new AirtableTable
            {
                Id = "tbl00000000000002",
                Name = "Contacts",
                Description = "A table for contacts",
                PrimaryFieldId = "fld00000000000005",
                Fields = new List<AirtableField>
                {
                    new() { Id = "fld00000000000005", Name = "Name", Type = AirtableFieldTypes.SingleLineText },
                    new() { Id = "fld00000000000006", Name = "Email", Type = AirtableFieldTypes.Email },
                    new() { Id = "fld00000000000007", Name = "Phone", Type = AirtableFieldTypes.PhoneNumber }
                },
                Views = new List<AirtableView>
                {
                    new() { Id = "viw00000000000002", Name = "Grid view", Type = AirtableViewTypes.Grid }
                }
            };

            Tables[defaultBase.Id][contactsTable.Id] = contactsTable;
            Records[defaultBase.Id][contactsTable.Id] = new Dictionary<string, AirtableRecord>();
        }

        #region ID Generation

        public string GenerateBaseId() => $"app{_baseCounter++:00000000000000}";
        public string GenerateTableId() => $"tbl{_tableCounter++:00000000000000}";
        public string GenerateFieldId() => $"fld{_fieldCounter++:00000000000000}";
        public string GenerateViewId() => $"viw{_viewCounter++:00000000000000}";
        public string GenerateRecordId() => $"rec{_recordCounter++:00000000000000}";
        public string GenerateCommentId() => $"com{_commentCounter++:00000000000000}";
        public string GenerateWebhookId() => $"ach{_webhookCounter++:00000000000000}";
        public string GenerateUserId() => $"usr{_userCounter++:00000000000000}";
        public string GenerateAttachmentId() => $"att{_attachmentCounter++:00000000000000}";

        #endregion

        #region Base Operations

        public AirtableBase CreateBase(string name)
        {
            var baseEntity = new AirtableBase
            {
                Id = GenerateBaseId(),
                Name = name,
                PermissionLevel = "create"
            };
            Bases[baseEntity.Id] = baseEntity;
            Tables[baseEntity.Id] = new Dictionary<string, AirtableTable>();
            Records[baseEntity.Id] = new Dictionary<string, Dictionary<string, AirtableRecord>>();
            Webhooks[baseEntity.Id] = new Dictionary<string, AirtableWebhook>();
            return baseEntity;
        }

        public AirtableBase? GetBase(string baseId)
        {
            return Bases.TryGetValue(baseId, out var baseEntity) ? baseEntity : null;
        }

        #endregion

        #region Table Operations

        public AirtableTable CreateTable(string baseId, string name, string? description = null, List<AirtableField>? fields = null)
        {
            if (!Tables.ContainsKey(baseId))
            {
                Tables[baseId] = new Dictionary<string, AirtableTable>();
                Records[baseId] = new Dictionary<string, Dictionary<string, AirtableRecord>>();
            }

            var tableFields = fields ?? new List<AirtableField>
            {
                new() { Id = GenerateFieldId(), Name = "Name", Type = AirtableFieldTypes.SingleLineText }
            };

            var table = new AirtableTable
            {
                Id = GenerateTableId(),
                Name = name,
                Description = description,
                PrimaryFieldId = tableFields.FirstOrDefault()?.Id ?? "",
                Fields = tableFields,
                Views = new List<AirtableView>
                {
                    new() { Id = GenerateViewId(), Name = "Grid view", Type = AirtableViewTypes.Grid }
                }
            };

            Tables[baseId][table.Id] = table;
            Records[baseId][table.Id] = new Dictionary<string, AirtableRecord>();
            return table;
        }

        public AirtableTable? GetTable(string baseId, string tableIdOrName)
        {
            if (!Tables.TryGetValue(baseId, out var baseTables))
                return null;

            // Try by ID first
            if (baseTables.TryGetValue(tableIdOrName, out var table))
                return table;

            // Try by name
            return baseTables.Values.FirstOrDefault(t =>
                t.Name.Equals(tableIdOrName, StringComparison.OrdinalIgnoreCase));
        }

        public List<AirtableTable> GetTables(string baseId)
        {
            return Tables.TryGetValue(baseId, out var baseTables)
                ? baseTables.Values.ToList()
                : new List<AirtableTable>();
        }

        public AirtableTable? UpdateTable(string baseId, string tableIdOrName, string? name = null, string? description = null)
        {
            var table = GetTable(baseId, tableIdOrName);
            if (table == null) return null;

            if (name != null) table.Name = name;
            if (description != null) table.Description = description;

            return table;
        }

        #endregion

        #region Field Operations

        public AirtableField? CreateField(string baseId, string tableIdOrName, string name, string type)
        {
            var table = GetTable(baseId, tableIdOrName);
            if (table == null) return null;

            var field = new AirtableField
            {
                Id = GenerateFieldId(),
                Name = name,
                Type = type
            };

            table.Fields.Add(field);
            return field;
        }

        public AirtableField? GetField(string baseId, string tableIdOrName, string fieldIdOrName)
        {
            var table = GetTable(baseId, tableIdOrName);
            if (table == null) return null;

            return table.Fields.FirstOrDefault(f =>
                f.Id == fieldIdOrName ||
                f.Name.Equals(fieldIdOrName, StringComparison.OrdinalIgnoreCase));
        }

        public AirtableField? UpdateField(string baseId, string tableIdOrName, string fieldIdOrName, string? name = null, string? description = null)
        {
            var field = GetField(baseId, tableIdOrName, fieldIdOrName);
            if (field == null) return null;

            if (name != null) field.Name = name;
            if (description != null) field.Description = description;

            return field;
        }

        #endregion

        #region View Operations

        public AirtableView? GetView(string baseId, string viewId)
        {
            if (!Tables.TryGetValue(baseId, out var baseTables))
                return null;

            foreach (var table in baseTables.Values)
            {
                var view = table.Views.FirstOrDefault(v => v.Id == viewId);
                if (view != null) return view;
            }

            return null;
        }

        public List<AirtableView> GetViews(string baseId)
        {
            if (!Tables.TryGetValue(baseId, out var baseTables))
                return new List<AirtableView>();

            return baseTables.Values.SelectMany(t => t.Views).ToList();
        }

        public bool DeleteView(string baseId, string viewId)
        {
            if (!Tables.TryGetValue(baseId, out var baseTables))
                return false;

            foreach (var table in baseTables.Values)
            {
                var view = table.Views.FirstOrDefault(v => v.Id == viewId);
                if (view != null)
                {
                    table.Views.Remove(view);
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Record Operations

        public AirtableRecord CreateRecord(string baseId, string tableIdOrName, Dictionary<string, object?> fields)
        {
            var table = GetTable(baseId, tableIdOrName);
            if (table == null)
                throw new InvalidOperationException($"Table '{tableIdOrName}' not found in base '{baseId}'");

            var tableId = table.Id;
            if (!Records.ContainsKey(baseId))
                Records[baseId] = new Dictionary<string, Dictionary<string, AirtableRecord>>();
            if (!Records[baseId].ContainsKey(tableId))
                Records[baseId][tableId] = new Dictionary<string, AirtableRecord>();

            var record = new AirtableRecord
            {
                Id = GenerateRecordId(),
                CreatedTime = DateTime.UtcNow.ToString("o"),
                Fields = fields
            };

            Records[baseId][tableId][record.Id] = record;
            
            GenerateWebhookPayload(baseId, tableId, "create", record);
            
            return record;
        }

        public AirtableRecord? GetRecord(string baseId, string tableIdOrName, string recordId)
        {
            var table = GetTable(baseId, tableIdOrName);
            if (table == null) return null;

            if (!Records.TryGetValue(baseId, out var baseRecords))
                return null;
            if (!baseRecords.TryGetValue(table.Id, out var tableRecords))
                return null;

            return tableRecords.TryGetValue(recordId, out var record) ? record : null;
        }

        public List<AirtableRecord> GetRecords(string baseId, string tableIdOrName, int? pageSize = null, string? offset = null)
        {
            var table = GetTable(baseId, tableIdOrName);
            if (table == null) return new List<AirtableRecord>();

            if (!Records.TryGetValue(baseId, out var baseRecords))
                return new List<AirtableRecord>();
            if (!baseRecords.TryGetValue(table.Id, out var tableRecords))
                return new List<AirtableRecord>();

            var records = tableRecords.Values.ToList();

            // Apply offset (simple integer-based for this simulation)
            if (offset != null && int.TryParse(offset, out var offsetInt))
            {
                records = records.Skip(offsetInt).ToList();
            }

            // Apply page size
            if (pageSize.HasValue && pageSize.Value > 0)
            {
                records = records.Take(pageSize.Value).ToList();
            }

            return records;
        }

        public AirtableRecord? UpdateRecord(string baseId, string tableIdOrName, string recordId, Dictionary<string, object?> fields, bool destructive = false)
        {
            var table = GetTable(baseId, tableIdOrName);
            if (table == null) return null;

            var record = GetRecord(baseId, tableIdOrName, recordId);
            if (record == null) return null;

            if (destructive)
            {
                record.Fields = fields;
            }
            else
            {
                foreach (var kvp in fields)
                {
                    record.Fields[kvp.Key] = kvp.Value;
                }
            }
            
            GenerateWebhookPayload(baseId, table.Id, "update", record);

            return record;
        }

        public bool DeleteRecord(string baseId, string tableIdOrName, string recordId)
        {
            var table = GetTable(baseId, tableIdOrName);
            if (table == null) return false;

            if (!Records.TryGetValue(baseId, out var baseRecords))
                return false;
            if (!baseRecords.TryGetValue(table.Id, out var tableRecords))
                return false;

            var removed = tableRecords.Remove(recordId);
            
            if (removed)
            {
                GenerateWebhookPayload(baseId, table.Id, "delete", new Dictionary<string, string> { { "id", recordId } });
            }
            
            return removed;
        }

        #region Webhook Operations

        public AirtableWebhook CreateWebhook(string baseId, string? notificationUrl, AirtableWebhookSpecification? specification)
        {
            if (!Webhooks.ContainsKey(baseId))
                Webhooks[baseId] = new Dictionary<string, AirtableWebhook>();

            var webhook = new AirtableWebhook
            {
                Id = GenerateWebhookId(),
                NotificationUrl = notificationUrl,
                MacSecretBase64 = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                Specification = specification,
                IsHookEnabled = true,
                ExpirationTime = DateTime.UtcNow.AddDays(7).ToString("o"),
                CursorForNextPayload = 1
            };

            Webhooks[baseId][webhook.Id] = webhook;
            WebhookPayloads[webhook.Id] = new List<AirtableWebhookPayload>();
            return webhook;
        }

        public List<AirtableWebhook> GetWebhooks(string baseId)
        {
            return Webhooks.TryGetValue(baseId, out var webhooks)
                ? webhooks.Values.ToList()
                : new List<AirtableWebhook>();
        }

        public AirtableWebhook? UpdateWebhook(string baseId, string webhookId, bool? enable)
        {
            if (!Webhooks.TryGetValue(baseId, out var webhooks))
                return null;

            if (!webhooks.TryGetValue(webhookId, out var webhook))
                return null;

            if (enable.HasValue)
                webhook.IsHookEnabled = enable.Value;

            return webhook;
        }

        public bool DeleteWebhook(string baseId, string webhookId)
        {
            if (!Webhooks.TryGetValue(baseId, out var webhooks))
                return false;

            if (webhooks.Remove(webhookId))
            {
                WebhookPayloads.Remove(webhookId);
                return true;
            }

            return false;
        }

        public List<AirtableWebhookPayload> GetWebhookPayloads(string webhookId, int? cursor = null)
        {
            if (!WebhookPayloads.TryGetValue(webhookId, out var payloads))
                return new List<AirtableWebhookPayload>();

            if (cursor.HasValue)
            {
                return payloads.Where(p => p.PayloadNumber >= cursor.Value).ToList();
            }

            return payloads;
        }

        private void GenerateWebhookPayload(string baseId, string tableId, string action, object data)
        {
            if (!Webhooks.TryGetValue(baseId, out var webhooks) || !webhooks.Any())
                return;

            foreach (var webhook in webhooks.Values)
            {
                if (!webhook.IsHookEnabled) continue;

                // Simple specification check (can be expanded)
                if (webhook.Specification?.Options?.Filters?.DataTypes != null && 
                    !webhook.Specification.Options.Filters.DataTypes.Contains("tableData"))
                {
                    continue;
                }

                var changedTablesById = new JObject();
                var tableObj = new JObject();

                if (action == "create" && data is AirtableRecord createdRecord)
                {
                    var createdRecordsObj = new JObject();
                    var recordObj = new JObject();
                    
                    // Simple cell values (only fields present)
                    var cellValues = new JObject();
                    foreach(var kvp in createdRecord.Fields)
                    {
                        if (kvp.Value != null)
                            cellValues[kvp.Key] = JToken.FromObject(kvp.Value);
                    }
                    recordObj["cellValuesByFieldId"] = cellValues;
                    
                    createdRecordsObj[createdRecord.Id] = recordObj;
                    tableObj["createdRecordsById"] = createdRecordsObj;
                }
                else if (action == "update" && data is AirtableRecord updatedRecord)
                {
                    var changedRecordsObj = new JObject();
                    var recordObj = new JObject();
                    
                    var currentObj = new JObject();
                    var cellValues = new JObject();
                    foreach(var kvp in updatedRecord.Fields)
                    {
                        if (kvp.Value != null)
                            cellValues[kvp.Key] = JToken.FromObject(kvp.Value);
                    }
                    currentObj["cellValuesByFieldId"] = cellValues;
                    recordObj["current"] = currentObj;

                    // Previous values not tracked in this simple simulation
                    recordObj["previous"] = new JObject(); 

                    changedRecordsObj[updatedRecord.Id] = recordObj;
                    tableObj["changedRecordsById"] = changedRecordsObj;
                }
                else if (action == "delete" && data is  Dictionary<string, string> deleteInfo)
                {
                    var destroyedIds = new JArray();
                    destroyedIds.Add(deleteInfo["id"]);
                    tableObj["destroyedRecordIds"] = destroyedIds;
                }

                changedTablesById[tableId] = tableObj;

                var payload = new AirtableWebhookPayload
                {
                    Timestamp = DateTime.UtcNow.ToString("o"),
                    BaseTransactionNumber = DateTime.UtcNow.Ticks.ToString(), // Mock transaction number
                    PayloadNumber = webhook.CursorForNextPayload++,
                    ActionMetadata = JObject.FromObject(new { source = "publicApi" }),
                    ChangedTablesById = changedTablesById
                };

                if (!WebhookPayloads.ContainsKey(webhook.Id))
                    WebhookPayloads[webhook.Id] = new List<AirtableWebhookPayload>();
                
                WebhookPayloads[webhook.Id].Add(payload);
            }
        }

        #endregion

        #endregion

        #region Comment Operations

        private string GetRecordKey(string baseId, string tableId, string recordId) =>
            $"{baseId}:{tableId}:{recordId}";

        public AirtableComment CreateComment(string baseId, string tableIdOrName, string recordId, string text)
        {
            var table = GetTable(baseId, tableIdOrName);
            if (table == null)
                throw new InvalidOperationException($"Table '{tableIdOrName}' not found");

            var recordKey = GetRecordKey(baseId, table.Id, recordId);
            if (!Comments.ContainsKey(recordKey))
                Comments[recordKey] = new Dictionary<string, AirtableComment>();

            var comment = new AirtableComment
            {
                Id = GenerateCommentId(),
                Author = new AirtableCommentAuthor
                {
                    Id = CurrentUser.Id,
                    Email = CurrentUser.Email ?? "",
                    Name = "Test User"
                },
                Text = text,
                CreatedTime = DateTime.UtcNow.ToString("o")
            };

            Comments[recordKey][comment.Id] = comment;
            return comment;
        }

        public List<AirtableComment> GetComments(string baseId, string tableIdOrName, string recordId)
        {
            var table = GetTable(baseId, tableIdOrName);
            if (table == null) return new List<AirtableComment>();

            var recordKey = GetRecordKey(baseId, table.Id, recordId);
            return Comments.TryGetValue(recordKey, out var comments)
                ? comments.Values.ToList()
                : new List<AirtableComment>();
        }

        public AirtableComment? UpdateComment(string baseId, string tableIdOrName, string recordId, string commentId, string text)
        {
            var table = GetTable(baseId, tableIdOrName);
            if (table == null) return null;

            var recordKey = GetRecordKey(baseId, table.Id, recordId);
            if (!Comments.TryGetValue(recordKey, out var comments))
                return null;

            if (!comments.TryGetValue(commentId, out var comment))
                return null;

            comment.Text = text;
            comment.LastModifiedTime = DateTime.UtcNow.ToString("o");
            return comment;
        }

        public bool DeleteComment(string baseId, string tableIdOrName, string recordId, string commentId)
        {
            var table = GetTable(baseId, tableIdOrName);
            if (table == null) return false;

            var recordKey = GetRecordKey(baseId, table.Id, recordId);
            if (!Comments.TryGetValue(recordKey, out var comments))
                return false;

            return comments.Remove(commentId);
        }

        #endregion


    }
}
