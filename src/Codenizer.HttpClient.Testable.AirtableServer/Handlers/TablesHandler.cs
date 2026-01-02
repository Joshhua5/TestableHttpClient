using Codenizer.HttpClient.Testable.AirtableServer.Models;
using Newtonsoft.Json.Linq;

namespace Codenizer.HttpClient.Testable.AirtableServer.Handlers
{
    /// <summary>
    /// Handles Airtable Tables API endpoints.
    /// </summary>
    public class TablesHandler
    {
        private readonly AirtableState _state;

        public TablesHandler(AirtableState state)
        {
            _state = state;
        }

        /// <summary>
        /// List tables in a base.
        /// GET /v0/meta/bases/{baseId}/tables
        /// </summary>
        public object List(string baseId)
        {
            var baseEntity = _state.GetBase(baseId);
            if (baseEntity == null)
            {
                return new { error = new { type = "NOT_FOUND", message = $"Base '{baseId}' not found" } };
            }

            var tables = _state.GetTables(baseId);
            return new { tables };
        }

        /// <summary>
        /// Create a new table.
        /// POST /v0/meta/bases/{baseId}/tables
        /// </summary>
        public object Create(string baseId, JObject body)
        {
            var baseEntity = _state.GetBase(baseId);
            if (baseEntity == null)
            {
                return new { error = new { type = "NOT_FOUND", message = $"Base '{baseId}' not found" } };
            }

            var name = body["name"]?.ToString();
            if (string.IsNullOrEmpty(name))
            {
                return new { error = new { type = "INVALID_REQUEST_UNKNOWN", message = "Table name is required" } };
            }

            var description = body["description"]?.ToString();

            // Parse fields if provided
            List<AirtableField>? fields = null;
            if (body.TryGetValue("fields", out var fieldsToken) && fieldsToken is JArray fieldsArray)
            {
                fields = new List<AirtableField>();
                foreach (var fieldToken in fieldsArray)
                {
                    var fieldObj = fieldToken as JObject;
                    var fieldName = fieldObj?["name"]?.ToString() ?? "Field";
                    var fieldType = fieldObj?["type"]?.ToString() ?? AirtableFieldTypes.SingleLineText;
                    
                    fields.Add(new AirtableField
                    {
                        Id = _state.GenerateFieldId(),
                        Name = fieldName,
                        Type = fieldType,
                        Description = fieldObj?["description"]?.ToString(),
                        Options = fieldObj?["options"] as JObject
                    });
                }
            }

            var table = _state.CreateTable(baseId, name, description, fields);
            return table;
        }

        /// <summary>
        /// Update a table.
        /// PATCH /v0/meta/bases/{baseId}/tables/{tableIdOrName}
        /// </summary>
        public object Update(string baseId, string tableIdOrName, JObject body)
        {
            var baseEntity = _state.GetBase(baseId);
            if (baseEntity == null)
            {
                return new { error = new { type = "NOT_FOUND", message = $"Base '{baseId}' not found" } };
            }

            var name = body["name"]?.ToString();
            var description = body["description"]?.ToString();

            var table = _state.UpdateTable(baseId, tableIdOrName, name, description);
            if (table == null)
            {
                return new { error = new { type = "NOT_FOUND", message = $"Table '{tableIdOrName}' not found" } };
            }

            return table;
        }
    }
}
