namespace Codenizer.HttpClient.Testable.AirtableServer.Handlers
{
    /// <summary>
    /// Handles Airtable Bases API endpoints.
    /// </summary>
    public class BasesHandler
    {
        private readonly AirtableState _state;

        public BasesHandler(AirtableState state)
        {
            _state = state;
        }

        /// <summary>
        /// List all bases accessible to the user.
        /// GET /v0/meta/bases
        /// </summary>
        public object List(Dictionary<string, string> queryParams)
        {
            var bases = _state.Bases.Values.Select(b => new
            {
                b.Id,
                b.Name,
                b.PermissionLevel
            }).ToList();

            // Handle pagination
            int offset = 0;
            if (queryParams.TryGetValue("offset", out var off) && int.TryParse(off, out var offVal))
            {
                offset = offVal;
            }

            var paginatedBases = bases.Skip(offset).Take(100).ToList();
            var hasMore = bases.Count > offset + 100;

            var result = new Dictionary<string, object>
            {
                { "bases", paginatedBases }
            };

            if (hasMore)
            {
                result["offset"] = (offset + 100).ToString();
            }

            return result;
        }

        /// <summary>
        /// Get base schema (tables, fields, views).
        /// GET /v0/meta/bases/{baseId}/tables
        /// </summary>
        public object GetSchema(string baseId)
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
        /// Get base metadata.
        /// GET /v0/meta/bases/{baseId}
        /// </summary>
        public object GetMetadata(string baseId)
        {
            var baseEntity = _state.GetBase(baseId);
            if (baseEntity == null)
            {
                return new { error = new { type = "NOT_FOUND", message = $"Base '{baseId}' not found" } };
            }

            return baseEntity;
        }
    }
}
