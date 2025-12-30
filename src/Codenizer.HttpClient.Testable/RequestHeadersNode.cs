using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Codenizer.HttpClient.Testable
{
    internal class RequestHeadersNode : RequestNode
    {
        private readonly Dictionary<string, string> _headers;
        private readonly List<RequestCookieNode> _requestCookieNodes = new();

        public RequestHeadersNode(Dictionary<string, string> headers)
        {
            _headers = headers;
        }

        public RequestBuilder? RequestBuilder { get; private set; }

        public bool Matches(Dictionary<string, string> headers)
        {
            if (_headers.Count != headers.Count)
            {
                return false;
            }

            foreach (var kv in _headers)
            {
                if (!headers.TryGetValue(kv.Key, out var value) || value != kv.Value)
                {
                    return false;
                }
            }

            return true;
        }

        public bool Match(HttpRequestHeaders headers)
        {
            foreach (var kv in _headers)
            {
                if (headers.TryGetValues(kv.Key, out var values))
                {
                    var found = false;
                    foreach (var v in values)
                    {
                        if (string.Equals(v, kv.Value, System.StringComparison.OrdinalIgnoreCase))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        return false;
                    }
                }
                else if (kv.Key == "Content-Type")
                {
                    // TODO: Remove this in a future release (2.4.x)
                    // This check is only here for backwards compatibility
                    // for the "feature" to automatically return HTTP 415 Unsupported Media Type
                    // when the Content-Type header value doesn't match what has been configured
                    // on that particular URI.
                    // This should be configured by the user of the library when their software
                    // depends on that behaviour from a server.
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<RequestCookieNode?> MatchCookiesAsync(MatchContext context)
        {
            foreach (var node in _requestCookieNodes)
            {
                if (await node.MatchAsync(context))
                {
                    return node;
                }
            }

            return null;
        }

        public override void Accept(RequestNodeVisitor visitor)
        {
            foreach (var header in _headers)
            {
                visitor.Header(header.Key, header.Value);
            }

            foreach (var node in _requestCookieNodes)
            {
                node.Accept(visitor);
            }
        }

        public RequestCookieNode Add(Dictionary<string, string> cookies)
        {
            var existingCookieNode = _requestCookieNodes.SingleOrDefault(node => node.Matches(cookies));

            if (existingCookieNode == null)
            {
                var requestCookieNode = new RequestCookieNode(cookies);

                _requestCookieNodes.Add(requestCookieNode);

                return requestCookieNode;
            }

            return existingCookieNode;
        }
    }
}