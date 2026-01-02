using Codenizer.HttpClient.Testable.AirtableServer.Models;
using Newtonsoft.Json.Linq;

namespace Codenizer.HttpClient.Testable.AirtableServer.Handlers
{
    /// <summary>
    /// Handles Airtable Webhooks API endpoints.
    /// </summary>
    public class WebhooksHandler
    {
        private readonly AirtableState _state;

        public WebhooksHandler(AirtableState state)
        {
            _state = state;
        }

        /// <summary>
        /// List webhooks for a base.
        /// GET /v0/bases/{baseId}/webhooks
        /// </summary>
        public object List(string baseId)
        {
            var baseEntity = _state.GetBase(baseId);
            if (baseEntity == null)
            {
                return new { error = new { type = "NOT_FOUND", message = $"Base '{baseId}' not found" } };
            }

            var webhooks = _state.GetWebhooks(baseId);
            return new { webhooks };
        }

        /// <summary>
        /// Create a new webhook.
        /// POST /v0/bases/{baseId}/webhooks
        /// </summary>
        public object Create(string baseId, JObject body)
        {
            var baseEntity = _state.GetBase(baseId);
            if (baseEntity == null)
            {
                return new { error = new { type = "NOT_FOUND", message = $"Base '{baseId}' not found" } };
            }

            var notificationUrl = body["notificationUrl"]?.ToString();

            AirtableWebhookSpecification? specification = null;
            if (body.TryGetValue("specification", out var specToken) && specToken is JObject specObj)
            {
                specification = new AirtableWebhookSpecification
                {
                    Options = ParseWebhookOptions(specObj["options"] as JObject)
                };
            }

            var webhook = _state.CreateWebhook(baseId, notificationUrl, specification);

            return new
            {
                webhook.Id,
                webhook.MacSecretBase64,
                webhook.ExpirationTime
            };
        }

        /// <summary>
        /// Enable or disable a webhook.
        /// PATCH /v0/bases/{baseId}/webhooks/{webhookId}
        /// </summary>
        public object Update(string baseId, string webhookId, JObject body)
        {
            var baseEntity = _state.GetBase(baseId);
            if (baseEntity == null)
            {
                return new { error = new { type = "NOT_FOUND", message = $"Base '{baseId}' not found" } };
            }

            bool? enable = null;
            if (body.TryGetValue("enable", out var enableToken))
            {
                enable = enableToken.Value<bool>();
            }

            var webhook = _state.UpdateWebhook(baseId, webhookId, enable);
            if (webhook == null)
            {
                return new { error = new { type = "NOT_FOUND", message = $"Webhook '{webhookId}' not found" } };
            }

            return webhook;
        }

        /// <summary>
        /// Delete a webhook.
        /// DELETE /v0/bases/{baseId}/webhooks/{webhookId}
        /// </summary>
        public object Delete(string baseId, string webhookId)
        {
            var baseEntity = _state.GetBase(baseId);
            if (baseEntity == null)
            {
                return new { error = new { type = "NOT_FOUND", message = $"Base '{baseId}' not found" } };
            }

            var deleted = _state.DeleteWebhook(baseId, webhookId);
            if (!deleted)
            {
                return new { error = new { type = "NOT_FOUND", message = $"Webhook '{webhookId}' not found" } };
            }

            return new { id = webhookId, deleted = true };
        }

        /// <summary>
        /// List webhook payloads.
        /// GET /v0/bases/{baseId}/webhooks/{webhookId}/payloads
        /// </summary>
        public object ListPayloads(string baseId, string webhookId, Dictionary<string, string> queryParams)
        {
            var baseEntity = _state.GetBase(baseId);
            if (baseEntity == null)
            {
                return new { error = new { type = "NOT_FOUND", message = $"Base '{baseId}' not found" } };
            }

            int? cursor = null;
            if (queryParams.TryGetValue("cursor", out var cur) && int.TryParse(cur, out var curVal))
            {
                cursor = curVal;
            }

            var payloads = _state.GetWebhookPayloads(webhookId, cursor);
            
            var result = new Dictionary<string, object>
            {
                { "payloads", payloads }
            };

            if (payloads.Any())
            {
                result["cursor"] = (payloads.Max(p => p.PayloadNumber) + 1);
                result["mightHaveMore"] = false;
            }

            return result;
        }

        private AirtableWebhookOptions? ParseWebhookOptions(JObject? optionsObj)
        {
            if (optionsObj == null) return null;

            return new AirtableWebhookOptions
            {
                Filters = ParseWebhookFilters(optionsObj["filters"] as JObject),
                Includes = optionsObj["includes"] as JObject
            };
        }

        private AirtableWebhookFilters? ParseWebhookFilters(JObject? filtersObj)
        {
            if (filtersObj == null) return null;

            return new AirtableWebhookFilters
            {
                DataTypes = filtersObj["dataTypes"]?.ToObject<List<string>>(),
                RecordChangeScope = filtersObj["recordChangeScope"]?.ToString(),
                ChangeTypes = filtersObj["changeTypes"]?.ToObject<List<string>>(),
                SourceOptions = filtersObj["sourceOptions"] as JObject
            };
        }
    }
}
