using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Codenizer.HttpClient.Testable
{
    internal class RequestCookieNode : RequestNode
    {
        private readonly Dictionary<string, string> _cookies;
        private readonly List<RequestContentNode> _requestContentNodes = new();

        public RequestCookieNode(Dictionary<string, string> cookies)
        {
            _cookies = cookies;
        }

        public bool Matches(Dictionary<string, string> cookies)
        {
            if (_cookies.Count != cookies.Count)
            {
                return false;
            }

            foreach (var kv in _cookies)
            {
                if (!cookies.TryGetValue(kv.Key, out var value) || value != kv.Value)
                {
                    return false;
                }
            }

            return true;
        }

        public bool Match(HttpRequestHeaders headers)
        {
            if (!_cookies.Any())
            {
                return true;
            }

            if (!headers.TryGetValues("Cookie", out var cookieValues))
            {
                return false;
            }

            var actualCookies = new Dictionary<string, string>();

            foreach (var headerValue in cookieValues)
            {
                var parts = headerValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    var kv = part.Split(new[] { '=' }, 2);
                    var key = kv[0].Trim();
                    var value = kv.Length > 1 ? kv[1].Trim() : "";
                    
                    if (actualCookies.ContainsKey(key))
                    {
                        actualCookies[key] = value;
                    }
                    else
                    {
                        actualCookies.Add(key, value);
                    }
                }
            }

            foreach (var expectedCookie in _cookies)
            {
                if (!actualCookies.TryGetValue(expectedCookie.Key, out var actualValue))
                {
                    return false;
                }

                if (!string.Equals(expectedCookie.Value, actualValue, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        public RequestContentNode? MatchContent(HttpContent? content)
        {
            return _requestContentNodes.SingleOrDefault(node => node.Match(content));
        }

        public RequestContentNode Add(string? expectedContent)
        {
            var existingContentNode = _requestContentNodes.SingleOrDefault(node => node.Match(expectedContent));

            if (existingContentNode == null)
            {
                var requestContentNode = new RequestContentNode(expectedContent);
                _requestContentNodes.Add(requestContentNode);
                return requestContentNode;
            }

            return existingContentNode;
        }

        public RequestContentNode Add(Func<HttpContent, bool> assertion)
        {
            // Predicates are unique, we cannot easily de-duplicate them ("semantic equality" of delegates is hard).
            // So we always add a new node.
            var requestContentNode = new RequestContentNode(assertion);
            _requestContentNodes.Add(requestContentNode);
            return requestContentNode;
        }

        public override void Accept(RequestNodeVisitor visitor)
        {
            // Visitor doesn't support Cookies yet, maybe add it later?
            // For now, cascade to content nodes.
            // Actually, I should probably update Visitor for "Response" cookies, but this is Request cookies.
            // RequestNodeVisitor doesn't have "Cookie" method.
            
            foreach (var node in _requestContentNodes)
            {
                node.Accept(visitor);
            }
        }
    }
}
