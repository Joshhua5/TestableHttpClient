using Codenizer.HttpClient.Testable.SlackServer.Models;

namespace Codenizer.HttpClient.Testable.SlackServer
{
    /// <summary>
    /// Manages the stateful data for the Slack emulator.
    /// </summary>
    public class SlackState
    {
        private long _timestampCounter = 1700000000000000;
        private int _channelCounter = 1;
        private int _messageCounter = 1;

        public SlackTeam Team { get; set; } = new()
        {
            Id = "T00000001",
            Name = "Test Workspace",
            Domain = "test-workspace",
            EmailDomain = "example.com"
        };

        public Dictionary<string, SlackUser> Users { get; } = new();
        public Dictionary<string, SlackChannel> Channels { get; } = new();
        public Dictionary<string, List<SlackMessage>> Messages { get; } = new();

        public string CurrentUserId { get; set; } = "U00000001";
        public string CurrentBotId { get; set; } = "B00000001";

        public SlackState()
        {
            SeedDefaultData();
        }

        private void SeedDefaultData()
        {
            // Add default users
            AddUser(new SlackUser
            {
                Id = "U00000001",
                Name = "testuser",
                RealName = "Test User",
                Email = "testuser@example.com",
                IsAdmin = true,
                Profile = new SlackUserProfile { DisplayName = "Test User", StatusText = "Working" }
            });

            AddUser(new SlackUser
            {
                Id = "U00000002",
                Name = "otheruser",
                RealName = "Other User",
                Email = "otheruser@example.com",
                Profile = new SlackUserProfile { DisplayName = "Other User" }
            });

            AddUser(new SlackUser
            {
                Id = "B00000001",
                Name = "slackbot",
                RealName = "Slackbot",
                IsBot = true,
                Profile = new SlackUserProfile { DisplayName = "Slackbot" }
            });

            // Add default channels
            AddChannel(new SlackChannel
            {
                Id = "C00000001",
                Name = "general",
                Creator = "U00000001",
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Members = new List<string> { "U00000001", "U00000002" },
                NumMembers = 2,
                IsMember = true,
                Topic = new SlackChannelTopic { Value = "Company-wide announcements" },
                Purpose = new SlackChannelPurpose { Value = "General discussion" }
            });

            AddChannel(new SlackChannel
            {
                Id = "C00000002",
                Name = "random",
                Creator = "U00000001",
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Members = new List<string> { "U00000001" },
                NumMembers = 1,
                IsMember = true,
                Topic = new SlackChannelTopic { Value = "Random stuff" }
            });
        }

        public void AddUser(SlackUser user)
        {
            user.Updated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Users[user.Id] = user;
        }

        public void AddChannel(SlackChannel channel)
        {
            Channels[channel.Id] = channel;
            if (!Messages.ContainsKey(channel.Id))
            {
                Messages[channel.Id] = new List<SlackMessage>();
            }
        }

        public SlackChannel CreateChannel(string name, string creatorId, bool isPrivate = false)
        {
            var channelId = $"C{_channelCounter++:00000000}";
            var channel = new SlackChannel
            {
                Id = channelId,
                Name = name,
                Creator = creatorId,
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                IsPrivate = isPrivate,
                IsMember = true,
                Members = new List<string> { creatorId },
                NumMembers = 1
            };
            AddChannel(channel);
            return channel;
        }

        public SlackMessage PostMessage(string channelId, string userId, string text, string? threadTs = null)
        {
            var ts = GenerateTimestamp();
            var message = new SlackMessage
            {
                Ts = ts,
                User = userId,
                Text = text,
                Channel = channelId,
                ThreadTs = threadTs
            };

            if (!Messages.ContainsKey(channelId))
            {
                Messages[channelId] = new List<SlackMessage>();
            }

            Messages[channelId].Add(message);
            return message;
        }

        public SlackMessage? UpdateMessage(string channelId, string ts, string text)
        {
            if (!Messages.TryGetValue(channelId, out var channelMessages))
            {
                return null;
            }

            var message = channelMessages.FirstOrDefault(m => m.Ts == ts);
            if (message != null)
            {
                message.Text = text;
                message.Edited = new SlackMessageEdited
                {
                    User = CurrentUserId,
                    Ts = GenerateTimestamp()
                };
            }

            return message;
        }

        public bool DeleteMessage(string channelId, string ts)
        {
            if (!Messages.TryGetValue(channelId, out var channelMessages))
            {
                return false;
            }

            var message = channelMessages.FirstOrDefault(m => m.Ts == ts);
            if (message != null)
            {
                channelMessages.Remove(message);
                return true;
            }

            return false;
        }

        public string GenerateTimestamp()
        {
            var ts = _timestampCounter++;
            return $"{ts / 1000000}.{ts % 1000000:000000}";
        }

        // Files
        public Dictionary<string, SlackFile> Files { get; } = new();
        private int _fileCounter = 1;

        public SlackFile UploadFile(string filename, string title, string content, List<string>? channels = null)
        {
            var fileId = $"F{_fileCounter++:00000000}";
            var file = new SlackFile
            {
                Id = fileId,
                Name = filename,
                Title = title,
                Mimetype = "text/plain",
                Filetype = "text",
                Size = content.Length,
                User = CurrentUserId,
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Channels = channels ?? new List<string>(),
                UrlPrivate = $"https://files.slack.com/files-pri/{Team.Id}-{fileId}/{filename}",
                Permalink = $"https://{Team.Domain}.slack.com/files/{CurrentUserId}/{fileId}/{filename}"
            };
            Files[fileId] = file;
            return file;
        }

        // Pins
        public Dictionary<string, List<SlackPin>> Pins { get; } = new();

        public void AddPin(string channelId, SlackPin pin)
        {
            if (!Pins.ContainsKey(channelId))
                Pins[channelId] = new List<SlackPin>();
            Pins[channelId].Add(pin);
        }

        public bool RemovePin(string channelId, string messageTs)
        {
            if (!Pins.TryGetValue(channelId, out var pins))
                return false;
            var pin = pins.FirstOrDefault(p => p.Message == messageTs);
            if (pin != null)
            {
                pins.Remove(pin);
                return true;
            }
            return false;
        }

        public List<SlackPin> GetPins(string channelId)
        {
            return Pins.TryGetValue(channelId, out var pins) ? pins : new List<SlackPin>();
        }

        // Bookmarks
        public Dictionary<string, List<SlackBookmark>> Bookmarks { get; } = new();
        private int _bookmarkCounter = 1;

        public SlackBookmark AddBookmark(string channelId, string title, string link, string emoji = "")
        {
            var bookmarkId = $"Bk{_bookmarkCounter++:00000000}";
            var bookmark = new SlackBookmark
            {
                Id = bookmarkId,
                ChannelId = channelId,
                Title = title,
                Link = link,
                Emoji = emoji,
                DateCreated = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                DateUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            if (!Bookmarks.ContainsKey(channelId))
                Bookmarks[channelId] = new List<SlackBookmark>();
            Bookmarks[channelId].Add(bookmark);
            return bookmark;
        }

        public SlackBookmark? GetBookmark(string channelId, string bookmarkId)
        {
            if (!Bookmarks.TryGetValue(channelId, out var bookmarks))
                return null;
            return bookmarks.FirstOrDefault(b => b.Id == bookmarkId);
        }

        public bool RemoveBookmark(string channelId, string bookmarkId)
        {
            if (!Bookmarks.TryGetValue(channelId, out var bookmarks))
                return false;
            var bookmark = bookmarks.FirstOrDefault(b => b.Id == bookmarkId);
            if (bookmark != null)
            {
                bookmarks.Remove(bookmark);
                return true;
            }
            return false;
        }

        public List<SlackBookmark> GetBookmarks(string channelId)
        {
            return Bookmarks.TryGetValue(channelId, out var bookmarks) ? bookmarks : new List<SlackBookmark>();
        }

        // Custom Emoji
        public Dictionary<string, string> CustomEmoji { get; } = new()
        {
            { "party_parrot", "https://emoji.slack-edge.com/T00000001/party_parrot/animation.gif" },
            { "thumbsup_all", "alias:+1" },
            { "shipit", "https://emoji.slack-edge.com/T00000001/shipit/shipit.png" }
        };
    }
}
