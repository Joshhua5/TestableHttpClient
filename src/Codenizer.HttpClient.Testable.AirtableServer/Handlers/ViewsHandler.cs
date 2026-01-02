namespace Codenizer.HttpClient.Testable.AirtableServer.Handlers
{
    /// <summary>
    /// Handles Airtable Views API endpoints.
    /// </summary>
    public class ViewsHandler
    {
        private readonly AirtableState _state;

        public ViewsHandler(AirtableState state)
        {
            _state = state;
        }

        /// <summary>
        /// List all views in a base.
        /// GET /v0/meta/bases/{baseId}/views
        /// </summary>
        public object List(string baseId)
        {
            var baseEntity = _state.GetBase(baseId);
            if (baseEntity == null)
            {
                return new { error = new { type = "NOT_FOUND", message = $"Base '{baseId}' not found" } };
            }

            var views = _state.GetViews(baseId);
            return new { views };
        }

        /// <summary>
        /// Get a specific view's metadata.
        /// GET /v0/meta/bases/{baseId}/views/{viewId}
        /// </summary>
        public object Get(string baseId, string viewId)
        {
            var baseEntity = _state.GetBase(baseId);
            if (baseEntity == null)
            {
                return new { error = new { type = "NOT_FOUND", message = $"Base '{baseId}' not found" } };
            }

            var view = _state.GetView(baseId, viewId);
            if (view == null)
            {
                return new { error = new { type = "NOT_FOUND", message = $"View '{viewId}' not found" } };
            }

            return view;
        }

        /// <summary>
        /// Delete a view.
        /// DELETE /v0/meta/bases/{baseId}/views/{viewId}
        /// </summary>
        public object Delete(string baseId, string viewId)
        {
            var baseEntity = _state.GetBase(baseId);
            if (baseEntity == null)
            {
                return new { error = new { type = "NOT_FOUND", message = $"Base '{baseId}' not found" } };
            }

            var deleted = _state.DeleteView(baseId, viewId);
            if (!deleted)
            {
                return new { error = new { type = "NOT_FOUND", message = $"View '{viewId}' not found" } };
            }

            return new { id = viewId, deleted = true };
        }
    }
}
