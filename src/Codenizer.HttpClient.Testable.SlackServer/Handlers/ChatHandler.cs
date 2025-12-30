namespace Codenizer.HttpClient.Testable.SlackServer.Handlers
{
    public class ChatHandler
    {
        private readonly SlackState _state;

        public ChatHandler(SlackState state)
        {
            _state = state;
        }

        public object PostMessage(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("channel", out var channelId))
                return new { ok = false, error = "channel_not_found" };

            if (!data.TryGetValue("text", out var text))
                return new { ok = false, error = "no_text" };

            if (!_state.Channels.ContainsKey(channelId))
                return new { ok = false, error = "channel_not_found" };

            var threadTs = data.TryGetValue("thread_ts", out var tts) ? tts : null;
            var message = _state.PostMessage(channelId, _state.CurrentBotId, text, threadTs);

            return new
            {
                ok = true,
                channel = channelId,
                ts = message.Ts,
                message = new
                {
                    type = "message",
                    subtype = "bot_message",
                    text = message.Text,
                    ts = message.Ts,
                    bot_id = _state.CurrentBotId
                }
            };
        }

        public object Update(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("channel", out var channelId))
                return new { ok = false, error = "channel_not_found" };

            if (!data.TryGetValue("ts", out var ts))
                return new { ok = false, error = "message_not_found" };

            if (!data.TryGetValue("text", out var text))
                return new { ok = false, error = "no_text" };

            var message = _state.UpdateMessage(channelId, ts, text);
            if (message == null)
                return new { ok = false, error = "message_not_found" };

            return new
            {
                ok = true,
                channel = channelId,
                ts = message.Ts,
                text = message.Text
            };
        }

        public object Delete(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("channel", out var channelId))
                return new { ok = false, error = "channel_not_found" };

            if (!data.TryGetValue("ts", out var ts))
                return new { ok = false, error = "message_not_found" };

            var success = _state.DeleteMessage(channelId, ts);
            if (!success)
                return new { ok = false, error = "message_not_found" };

            return new { ok = true, channel = channelId, ts };
        }

        public object PostEphemeral(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("channel", out var channelId))
                return new { ok = false, error = "channel_not_found" };

            if (!data.TryGetValue("user", out var userId))
                return new { ok = false, error = "user_not_found" };

            if (!data.TryGetValue("text", out var text))
                return new { ok = false, error = "no_text" };

            // Ephemeral messages are not stored in state
            var ts = _state.GenerateTimestamp();

            return new
            {
                ok = true,
                message_ts = ts
            };
        }
    }
}
