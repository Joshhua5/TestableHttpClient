using Codenizer.HttpClient.Testable.SlackServer.Models;

namespace Codenizer.HttpClient.Testable.SlackServer.Handlers
{
    public class ConversationsHandler
    {
        private readonly SlackState _state;

        public ConversationsHandler(SlackState state)
        {
            _state = state;
        }

        public object List(Dictionary<string, string> data)
        {
            var excludeArchived = data.TryGetValue("exclude_archived", out var ea) && ea == "true";
            var channels = _state.Channels.Values
                .Where(c => !excludeArchived || !c.IsArchived)
                .ToList();

            return new { ok = true, channels };
        }

        public object Info(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("channel", out var channelId))
                return new { ok = false, error = "channel_not_found" };

            if (!_state.Channels.TryGetValue(channelId, out var channel))
                return new { ok = false, error = "channel_not_found" };

            return new { ok = true, channel };
        }

        public object Create(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("name", out var name))
                return new { ok = false, error = "invalid_name" };

            var isPrivate = data.TryGetValue("is_private", out var ip) && ip == "true";
            var channel = _state.CreateChannel(name, _state.CurrentUserId, isPrivate);

            return new { ok = true, channel };
        }

        public object Archive(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("channel", out var channelId))
                return new { ok = false, error = "channel_not_found" };

            if (!_state.Channels.TryGetValue(channelId, out var channel))
                return new { ok = false, error = "channel_not_found" };

            channel.IsArchived = true;
            return new { ok = true };
        }

        public object Unarchive(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("channel", out var channelId))
                return new { ok = false, error = "channel_not_found" };

            if (!_state.Channels.TryGetValue(channelId, out var channel))
                return new { ok = false, error = "channel_not_found" };

            channel.IsArchived = false;
            return new { ok = true };
        }

        public object History(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("channel", out var channelId))
                return new { ok = false, error = "channel_not_found" };

            if (!_state.Messages.TryGetValue(channelId, out var messages))
                messages = new List<SlackMessage>();

            var limit = data.TryGetValue("limit", out var l) && int.TryParse(l, out var lVal) ? lVal : 100;
            var messageList = messages.OrderByDescending(m => m.Ts).Take(limit).ToList();

            return new { ok = true, messages = messageList, has_more = messages.Count > limit };
        }

        public object Members(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("channel", out var channelId))
                return new { ok = false, error = "channel_not_found" };

            if (!_state.Channels.TryGetValue(channelId, out var channel))
                return new { ok = false, error = "channel_not_found" };

            return new { ok = true, members = channel.Members };
        }

        public object Join(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("channel", out var channelId))
                return new { ok = false, error = "channel_not_found" };

            if (!_state.Channels.TryGetValue(channelId, out var channel))
                return new { ok = false, error = "channel_not_found" };

            if (!channel.Members.Contains(_state.CurrentUserId))
            {
                channel.Members.Add(_state.CurrentUserId);
                channel.NumMembers++;
            }
            channel.IsMember = true;

            return new { ok = true, channel };
        }

        public object Leave(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("channel", out var channelId))
                return new { ok = false, error = "channel_not_found" };

            if (!_state.Channels.TryGetValue(channelId, out var channel))
                return new { ok = false, error = "channel_not_found" };

            if (channel.Members.Contains(_state.CurrentUserId))
            {
                channel.Members.Remove(_state.CurrentUserId);
                channel.NumMembers = Math.Max(0, channel.NumMembers - 1);
            }
            channel.IsMember = false;

            return new { ok = true };
        }

        public object Invite(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("channel", out var channelId))
                return new { ok = false, error = "channel_not_found" };

            if (!data.TryGetValue("users", out var usersStr))
                return new { ok = false, error = "invalid_users" };

            if (!_state.Channels.TryGetValue(channelId, out var channel))
                return new { ok = false, error = "channel_not_found" };

            var userIds = usersStr.Split(',');
            foreach (var userId in userIds)
            {
                if (!channel.Members.Contains(userId))
                {
                    channel.Members.Add(userId);
                    channel.NumMembers++;
                }
            }

            return new { ok = true, channel };
        }

        public object Kick(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("channel", out var channelId))
                return new { ok = false, error = "channel_not_found" };

            if (!data.TryGetValue("user", out var userId))
                return new { ok = false, error = "invalid_users" };

            if (!_state.Channels.TryGetValue(channelId, out var channel))
                return new { ok = false, error = "channel_not_found" };

            if (channel.Members.Contains(userId))
            {
                channel.Members.Remove(userId);
                channel.NumMembers = Math.Max(0, channel.NumMembers - 1);
            }

            return new { ok = true };
        }

        public object Rename(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("channel", out var channelId))
                return new { ok = false, error = "channel_not_found" };

            if (!data.TryGetValue("name", out var name))
                return new { ok = false, error = "invalid_name" };

            if (!_state.Channels.TryGetValue(channelId, out var channel))
                return new { ok = false, error = "channel_not_found" };

            channel.Name = name;
            return new { ok = true, channel };
        }

        public object SetTopic(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("channel", out var channelId))
                return new { ok = false, error = "channel_not_found" };

            if (!data.TryGetValue("topic", out var topic))
                return new { ok = false, error = "invalid_topic" };

            if (!_state.Channels.TryGetValue(channelId, out var channel))
                return new { ok = false, error = "channel_not_found" };

            channel.Topic = new SlackChannelTopic
            {
                Value = topic,
                Creator = _state.CurrentUserId,
                LastSet = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            return new { ok = true, topic = channel.Topic.Value };
        }

        public object SetPurpose(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("channel", out var channelId))
                return new { ok = false, error = "channel_not_found" };

            if (!data.TryGetValue("purpose", out var purpose))
                return new { ok = false, error = "invalid_purpose" };

            if (!_state.Channels.TryGetValue(channelId, out var channel))
                return new { ok = false, error = "channel_not_found" };

            channel.Purpose = new SlackChannelPurpose
            {
                Value = purpose,
                Creator = _state.CurrentUserId,
                LastSet = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            return new { ok = true, purpose = channel.Purpose.Value };
        }
    }
}
