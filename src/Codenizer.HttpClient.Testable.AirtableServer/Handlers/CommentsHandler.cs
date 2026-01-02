using Newtonsoft.Json.Linq;

namespace Codenizer.HttpClient.Testable.AirtableServer.Handlers
{
    /// <summary>
    /// Handles Airtable Comments API endpoints.
    /// </summary>
    public class CommentsHandler
    {
        private readonly AirtableState _state;

        public CommentsHandler(AirtableState state)
        {
            _state = state;
        }

        /// <summary>
        /// List comments on a record.
        /// GET /v0/{baseId}/{tableIdOrName}/{recordId}/comments
        /// </summary>
        public object List(string baseId, string tableIdOrName, string recordId, Dictionary<string, string> queryParams)
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

            var comments = _state.GetComments(baseId, tableIdOrName, recordId);

            // Handle pagination
            int offset = 0;
            if (queryParams.TryGetValue("offset", out var off) && int.TryParse(off, out var offVal))
            {
                offset = offVal;
            }

            var paginatedComments = comments.Skip(offset).Take(100).ToList();
            var hasMore = comments.Count > offset + 100;

            var result = new Dictionary<string, object>
            {
                { "comments", paginatedComments }
            };

            if (hasMore)
            {
                result["offset"] = (offset + 100).ToString();
            }

            return result;
        }

        /// <summary>
        /// Create a comment on a record.
        /// POST /v0/{baseId}/{tableIdOrName}/{recordId}/comments
        /// </summary>
        public object Create(string baseId, string tableIdOrName, string recordId, JObject body)
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

            var text = body["text"]?.ToString();
            if (string.IsNullOrEmpty(text))
            {
                return new { error = new { type = "INVALID_REQUEST_UNKNOWN", message = "Comment text is required" } };
            }

            var comment = _state.CreateComment(baseId, tableIdOrName, recordId, text);
            return comment;
        }

        /// <summary>
        /// Update a comment.
        /// PATCH /v0/{baseId}/{tableIdOrName}/{recordId}/comments/{commentId}
        /// </summary>
        public object Update(string baseId, string tableIdOrName, string recordId, string commentId, JObject body)
        {
            var table = _state.GetTable(baseId, tableIdOrName);
            if (table == null)
            {
                return new { error = new { type = "TABLE_NOT_FOUND", message = $"Table '{tableIdOrName}' not found" } };
            }

            var text = body["text"]?.ToString();
            if (string.IsNullOrEmpty(text))
            {
                return new { error = new { type = "INVALID_REQUEST_UNKNOWN", message = "Comment text is required" } };
            }

            var comment = _state.UpdateComment(baseId, tableIdOrName, recordId, commentId, text);
            if (comment == null)
            {
                return new { error = new { type = "NOT_FOUND", message = $"Comment '{commentId}' not found" } };
            }

            return comment;
        }

        /// <summary>
        /// Delete a comment.
        /// DELETE /v0/{baseId}/{tableIdOrName}/{recordId}/comments/{commentId}
        /// </summary>
        public object Delete(string baseId, string tableIdOrName, string recordId, string commentId)
        {
            var table = _state.GetTable(baseId, tableIdOrName);
            if (table == null)
            {
                return new { error = new { type = "TABLE_NOT_FOUND", message = $"Table '{tableIdOrName}' not found" } };
            }

            var deleted = _state.DeleteComment(baseId, tableIdOrName, recordId, commentId);
            if (!deleted)
            {
                return new { error = new { type = "NOT_FOUND", message = $"Comment '{commentId}' not found" } };
            }

            return new { id = commentId, deleted = true };
        }
    }
}
