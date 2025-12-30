using Codenizer.HttpClient.Testable.SlackServer.Models;

namespace Codenizer.HttpClient.Testable.SlackServer.Handlers
{
    public class PinsHandler
    {
        private readonly SlackState _state;

        public PinsHandler(SlackState state)
        {
            _state = state;
        }

        public object Add(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("channel", out var channelId))
                return new { ok = false, error = "channel_not_found" };

            if (!data.TryGetValue("timestamp", out var ts))
                return new { ok = false, error = "message_not_found" };

            if (!_state.Channels.ContainsKey(channelId))
                return new { ok = false, error = "channel_not_found" };

            var pin = new SlackPin
            {
                Type = "message",
                Channel = channelId,
                Message = ts,
                CreatedBy = _state.CurrentUserId,
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            _state.AddPin(channelId, pin);

            return new { ok = true };
        }

        public object Remove(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("channel", out var channelId))
                return new { ok = false, error = "channel_not_found" };

            if (!data.TryGetValue("timestamp", out var ts))
                return new { ok = false, error = "no_pin" };

            var removed = _state.RemovePin(channelId, ts);
            if (!removed)
                return new { ok = false, error = "no_pin" };

            return new { ok = true };
        }

        public object List(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("channel", out var channelId))
                return new { ok = false, error = "channel_not_found" };

            var pins = _state.GetPins(channelId);

            var items = pins.Select(p => new
            {
                type = p.Type,
                created = p.Created,
                created_by = p.CreatedBy,
                message = p.Message != null ? new { ts = p.Message } : null
            }).ToList();

            return new { ok = true, items };
        }
    }
}
