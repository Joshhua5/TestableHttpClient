using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Codenizer.HttpClient.Testable
{
    internal class MatchContext
    {
        private string? _requestContent;
        private Dictionary<string, string>? _cookies;

        public MatchContext(HttpRequestMessage request)
        {
            Request = request;
        }

        public HttpRequestMessage Request { get; }

        public async Task<string?> GetRequestContentAsync()
        {
            if (_requestContent == null && Request.Content != null)
            {
                _requestContent = await Request.Content.ReadAsStringAsync();
            }

            return _requestContent;
        }

        public Dictionary<string, string> GetCookies()
        {
            if (_cookies == null)
            {
                _cookies = ParseCookies(Request.Headers);
            }

            return _cookies;
        }

        private static Dictionary<string, string> ParseCookies(System.Net.Http.Headers.HttpRequestHeaders headers)
        {
            var cookies = new Dictionary<string, string>();

            if (headers.TryGetValues("Cookie", out var cookieValues))
            {
                foreach (var headerValue in cookieValues)
                {
                    var parts = headerValue.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        var kv = part.Split(new[] { '=' }, 2);
                        var key = kv[0].Trim();
                        var value = kv.Length > 1 ? kv[1].Trim() : "";

                        if (cookies.ContainsKey(key))
                        {
                            cookies[key] = value;
                        }
                        else
                        {
                            cookies.Add(key, value);
                        }
                    }
                }
            }

            return cookies;
        }
    }
}
