using Codenizer.HttpClient.Testable.SlackServer.Models;

namespace Codenizer.HttpClient.Testable.SlackServer.Handlers
{
    public class FilesHandler
    {
        private readonly SlackState _state;

        public FilesHandler(SlackState state)
        {
            _state = state;
        }

        public object List(Dictionary<string, string> data)
        {
            var userId = data.TryGetValue("user", out var uid) ? uid : null;
            var channelId = data.TryGetValue("channel", out var cid) ? cid : null;

            var files = _state.Files.Values.AsEnumerable();

            if (userId != null)
                files = files.Where(f => f.User == userId);

            if (channelId != null)
                files = files.Where(f => f.Channels.Contains(channelId));

            return new { ok = true, files = files.ToList() };
        }

        public object Info(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("file", out var fileId))
                return new { ok = false, error = "file_not_found" };

            if (!_state.Files.TryGetValue(fileId, out var file))
                return new { ok = false, error = "file_not_found" };

            return new { ok = true, file };
        }

        public object Delete(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("file", out var fileId))
                return new { ok = false, error = "file_not_found" };

            if (!_state.Files.ContainsKey(fileId))
                return new { ok = false, error = "file_not_found" };

            _state.Files.Remove(fileId);
            return new { ok = true };
        }

        public object Upload(Dictionary<string, string> data)
        {
            var channels = data.TryGetValue("channels", out var ch) ? ch : null;
            var filename = data.TryGetValue("filename", out var fn) ? fn : "file.txt";
            var title = data.TryGetValue("title", out var t) ? t : filename;
            var content = data.TryGetValue("content", out var c) ? c : "";

            var file = _state.UploadFile(filename, title, content, channels?.Split(',').ToList());

            return new { ok = true, file };
        }

        public object SharedPublicURL(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("file", out var fileId))
                return new { ok = false, error = "file_not_found" };

            if (!_state.Files.TryGetValue(fileId, out var file))
                return new { ok = false, error = "file_not_found" };

            file.IsPublic = true;
            file.PermalinkPublic = $"https://slack-files.com/{fileId}/public";

            return new { ok = true, file };
        }

        public object RevokePublicURL(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("file", out var fileId))
                return new { ok = false, error = "file_not_found" };

            if (!_state.Files.TryGetValue(fileId, out var file))
                return new { ok = false, error = "file_not_found" };

            file.IsPublic = false;
            file.PermalinkPublic = null;

            return new { ok = true, file };
        }
    }
}
