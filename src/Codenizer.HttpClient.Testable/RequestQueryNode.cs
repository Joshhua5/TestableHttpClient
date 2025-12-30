using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Codenizer.HttpClient.Testable
{
    internal class RequestQueryNode : RequestNode
    {
        private readonly List<KeyValuePair<string, string?>> _queryParameters;
        private readonly List<QueryStringAssertion> _queryStringAssertions;
        private readonly List<RequestHeadersNode> _headersNodes = new List<RequestHeadersNode>();

        private static readonly List<KeyValuePair<string, string?>> EmptyQueryParameters = new List<KeyValuePair<string, string?>>();

        public RequestQueryNode(
            List<KeyValuePair<string, string?>> queryParameters,
            List<QueryStringAssertion> queryStringAssertions)
        {
            _queryParameters = queryParameters;
            _queryStringAssertions = queryStringAssertions;
        }

        public bool Matches(string? queryString)
        {
            if (string.IsNullOrEmpty(queryString))
            {
                return Matches(EmptyQueryParameters);
            }

            var inputQueryParameters = QueryParametersFrom(queryString).ToList();

            return Matches(inputQueryParameters);
        }

        public bool Matches(List<KeyValuePair<string, string?>> inputQueryParameters)
        {
            if (inputQueryParameters.Count != _queryParameters.Count)
            {
                // if the counts don't match then we're done
                return false;
            }

            for (var i = 0; i < inputQueryParameters.Count; i++)
            {
                var qp = inputQueryParameters[i];
                var found = false;

                // Check if the query parameter name exists at all
                for (var j = 0; j < _queryParameters.Count; j++)
                {
                    if (_queryParameters[j].Key == qp.Key)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
                
                // Check if there is a specific assertion for this particular parameter
                QueryStringAssertion? assertion = null;
                for (var j = 0; j < _queryStringAssertions.Count; j++)
                {
                    if (_queryStringAssertions[j].Key == qp.Key)
                    {
                        assertion = _queryStringAssertions[j];
                        break;
                    }
                }

                if (assertion != null)
                {
                    if (assertion.AnyValue)
                    {
                        // When there is an AnyValue assertion we do not have to check
                        // the value and can simply continue as we've already checked
                        // that this query parameter is present (see above)
                        continue;
                    }

                    // Potentially the assertion overrides the value of the query parameter
                    // in the originally configured URI, therefore we need to check the
                    // query parameter exists in the request with the value given in the assertion.
                    found = false;
                    for (var j = 0; j < inputQueryParameters.Count; j++)
                    {
                        if (inputQueryParameters[j].Key == qp.Key && inputQueryParameters[j].Value == assertion.Value)
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
                // When there is no assertion: check if the query parameter exists with the right name and value
                else
                {
                    found = false;
                    for (var j = 0; j < _queryParameters.Count; j++)
                    {
                        if (_queryParameters[j].Key == qp.Key && _queryParameters[j].Value == qp.Value)
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
            }

            return true;
        }

        private static IEnumerable<KeyValuePair<string, string?>> QueryParametersFrom(string? query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return EmptyQueryParameters;
            }

            return query!
                .Replace("?", "") // When using the Query property from Uri you will get a leading ?
                .Split('&')
                .Select(p => p.Split('='))
                .Select(p => new KeyValuePair<string, string?>(System.Uri.UnescapeDataString(p[0]),  p.Length == 2 ? System.Uri.UnescapeDataString(p[1]) : null));
        }

        public RequestHeadersNode Add(Dictionary<string, string> headers)
        {
            var existingHeaders = _headersNodes.SingleOrDefault(node => node.Matches(headers));

            if (existingHeaders == null)
            {
                existingHeaders = new RequestHeadersNode(headers);
                _headersNodes.Add(existingHeaders);
            }

            return existingHeaders;
        }

        public RequestHeadersNode? Match(HttpRequestHeaders headers)
        {
            return _headersNodes.SingleOrDefault(node => node.Match(headers));
        }
        
        public override void Accept(RequestNodeVisitor visitor)
        {
            if (_queryParameters.Any())
            {
                foreach (var qp in _queryParameters)
                {
                    var value = qp.Value;
                    var assertion = _queryStringAssertions.SingleOrDefault(a => a.Key == qp.Key);
                    if (assertion != null)
                    {
                        value = assertion.AnyValue ? "(any)" : assertion.Value;
                    }

                    visitor.QueryParameter(qp.Key, value);
                }
            }

            foreach (var node in _headersNodes)
            {
                node.Accept(visitor);
            }
        }
    }
}