using Codenizer.HttpClient.Testable.AirtableServer.Models;
using Newtonsoft.Json.Linq;

namespace Codenizer.HttpClient.Testable.AirtableServer.Handlers
{
    /// <summary>
    /// Handles Airtable Fields API endpoints.
    /// </summary>
    public class FieldsHandler
    {
        private readonly AirtableState _state;

        public FieldsHandler(AirtableState state)
        {
            _state = state;
        }

        /// <summary>
        /// Create a new field in a table.
        /// POST /v0/meta/bases/{baseId}/tables/{tableIdOrName}/fields
        /// </summary>
        public object Create(string baseId, string tableIdOrName, JObject body)
        {
            var table = _state.GetTable(baseId, tableIdOrName);
            if (table == null)
            {
                return new { error = new { type = "NOT_FOUND", message = $"Table '{tableIdOrName}' not found" } };
            }

            var name = body["name"]?.ToString();
            if (string.IsNullOrEmpty(name))
            {
                return new { error = new { type = "INVALID_REQUEST_UNKNOWN", message = "Field name is required" } };
            }

            var type = body["type"]?.ToString() ?? AirtableFieldTypes.SingleLineText;
            var field = _state.CreateField(baseId, tableIdOrName, name, type);

            if (field == null)
            {
                return new { error = new { type = "INVALID_REQUEST_UNKNOWN", message = "Failed to create field" } };
            }

            // Apply additional properties
            field.Description = body["description"]?.ToString();
            field.Options = body["options"] as JObject;

            return field;
        }

        /// <summary>
        /// Update a field.
        /// PATCH /v0/meta/bases/{baseId}/tables/{tableIdOrName}/fields/{fieldIdOrName}
        /// </summary>
        public object Update(string baseId, string tableIdOrName, string fieldIdOrName, JObject body)
        {
            var field = _state.GetField(baseId, tableIdOrName, fieldIdOrName);
            if (field == null)
            {
                return new { error = new { type = "NOT_FOUND", message = $"Field '{fieldIdOrName}' not found" } };
            }

            var name = body["name"]?.ToString();
            var description = body["description"]?.ToString();

            var updatedField = _state.UpdateField(baseId, tableIdOrName, fieldIdOrName, name, description);
            if (updatedField == null)
            {
                return new { error = new { type = "NOT_FOUND", message = $"Field '{fieldIdOrName}' not found" } };
            }

            return updatedField;
        }
    }
}
