using Codenizer.HttpClient.Testable.SlackServer.Models;

namespace Codenizer.HttpClient.Testable.SlackServer.Handlers
{
    public class ReactionsHandler
    {
        private readonly SlackState _state;

        public ReactionsHandler(SlackState state)
        {
            _state = state;
        }

        public object Add(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("channel", out var channelId))
                return new { ok = false, error = "channel_not_found" };

            if (!data.TryGetValue("timestamp", out var ts))
                return new { ok = false, error = "message_not_found" };

            if (!data.TryGetValue("name", out var name))
                return new { ok = false, error = "invalid_name" };

            if (!_state.Messages.TryGetValue(channelId, out var messages))
                return new { ok = false, error = "channel_not_found" };

            var message = messages.FirstOrDefault(m => m.Ts == ts);
            if (message == null)
                return new { ok = false, error = "message_not_found" };

            var reaction = message.Reactions.FirstOrDefault(r => r.Name == name);
            if (reaction == null)
            {
                reaction = new SlackReaction { Name = name, Count = 0, Users = new List<string>() };
                message.Reactions.Add(reaction);
            }

            if (!reaction.Users.Contains(_state.CurrentUserId))
            {
                reaction.Users.Add(_state.CurrentUserId);
                reaction.Count++;
            }

            return new { ok = true };
        }

        public object Remove(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("channel", out var channelId))
                return new { ok = false, error = "channel_not_found" };

            if (!data.TryGetValue("timestamp", out var ts))
                return new { ok = false, error = "message_not_found" };

            if (!data.TryGetValue("name", out var name))
                return new { ok = false, error = "invalid_name" };

            if (!_state.Messages.TryGetValue(channelId, out var messages))
                return new { ok = false, error = "channel_not_found" };

            var message = messages.FirstOrDefault(m => m.Ts == ts);
            if (message == null)
                return new { ok = false, error = "message_not_found" };

            var reaction = message.Reactions.FirstOrDefault(r => r.Name == name);
            if (reaction != null && reaction.Users.Contains(_state.CurrentUserId))
            {
                reaction.Users.Remove(_state.CurrentUserId);
                reaction.Count--;
                if (reaction.Count <= 0)
                {
                    message.Reactions.Remove(reaction);
                }
            }

            return new { ok = true };
        }

        public object Get(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("channel", out var channelId))
                return new { ok = false, error = "channel_not_found" };

            if (!data.TryGetValue("timestamp", out var ts))
                return new { ok = false, error = "message_not_found" };

            if (!_state.Messages.TryGetValue(channelId, out var messages))
                return new { ok = false, error = "channel_not_found" };

            var message = messages.FirstOrDefault(m => m.Ts == ts);
            if (message == null)
                return new { ok = false, error = "message_not_found" };

            return new
            {
                ok = true,
                type = "message",
                channel = channelId,
                message = new
                {
                    type = message.Type,
                    text = message.Text,
                    user = message.User,
                    ts = message.Ts,
                    reactions = message.Reactions
                }
            };
        }

        public object List(Dictionary<string, string> data)
        {
            var userId = data.TryGetValue("user", out var uid) ? uid : _state.CurrentUserId;
            var items = new List<object>();

            foreach (var channel in _state.Messages)
            {
                foreach (var message in channel.Value)
                {
                    if (message.Reactions.Any(r => r.Users.Contains(userId)))
                    {
                        items.Add(new
                        {
                            type = "message",
                            channel = channel.Key,
                            message = new
                            {
                                text = message.Text,
                                ts = message.Ts,
                                reactions = message.Reactions
                            }
                        });
                    }
                }
            }

            return new { ok = true, items };
        }
    }
}
