using Codenizer.HttpClient.Testable.SlackServer.Models;

namespace Codenizer.HttpClient.Testable.SlackServer.Handlers
{
    public class BookmarksHandler
    {
        private readonly SlackState _state;

        public BookmarksHandler(SlackState state)
        {
            _state = state;
        }

        public object Add(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("channel_id", out var channelId))
                return new { ok = false, error = "channel_not_found" };

            if (!data.TryGetValue("title", out var title))
                return new { ok = false, error = "invalid_title" };

            if (!data.TryGetValue("link", out var link))
                return new { ok = false, error = "invalid_link" };

            var emoji = data.TryGetValue("emoji", out var e) ? e : "";

            var bookmark = _state.AddBookmark(channelId, title, link, emoji);

            return new { ok = true, bookmark };
        }

        public object Edit(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("channel_id", out var channelId))
                return new { ok = false, error = "channel_not_found" };

            if (!data.TryGetValue("bookmark_id", out var bookmarkId))
                return new { ok = false, error = "bookmark_not_found" };

            var bookmark = _state.GetBookmark(channelId, bookmarkId);
            if (bookmark == null)
                return new { ok = false, error = "bookmark_not_found" };

            if (data.TryGetValue("title", out var title))
                bookmark.Title = title;

            if (data.TryGetValue("link", out var link))
                bookmark.Link = link;

            if (data.TryGetValue("emoji", out var emoji))
                bookmark.Emoji = emoji;

            bookmark.DateUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            return new { ok = true, bookmark };
        }

        public object Remove(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("channel_id", out var channelId))
                return new { ok = false, error = "channel_not_found" };

            if (!data.TryGetValue("bookmark_id", out var bookmarkId))
                return new { ok = false, error = "bookmark_not_found" };

            var removed = _state.RemoveBookmark(channelId, bookmarkId);
            if (!removed)
                return new { ok = false, error = "bookmark_not_found" };

            return new { ok = true };
        }

        public object List(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("channel_id", out var channelId))
                return new { ok = false, error = "channel_not_found" };

            var bookmarks = _state.GetBookmarks(channelId);

            return new { ok = true, bookmarks };
        }
    }
}
