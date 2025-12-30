using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Codenizer.HttpClient.Testable;

internal class RequestContentNode : RequestNode
{
    private readonly string? _expectedContent;
    private readonly Func<HttpContent, bool>? _assertion;

    public RequestContentNode(string? expectedContent)
    {
        _expectedContent = expectedContent;
    }

    public RequestContentNode(Func<HttpContent, bool> assertion)
    {
        _assertion = assertion;
    }
    
    public override void Accept(RequestNodeVisitor visitor)
    {
        if (_expectedContent != null)
        {
            visitor.Content(_expectedContent);
        }
        
        if (RequestBuilder != null)
        {
            visitor.Response(RequestBuilder);
        }
    }

    public RequestBuilder? RequestBuilder { get; private set; }

    public void SetRequestBuilder(RequestBuilder requestBuilder)
    {
        if (RequestBuilder != null)
        {
            throw new MultipleResponsesConfiguredException(2, requestBuilder.PathAndQuery!);
        }

        RequestBuilder = requestBuilder;
    }

    public async Task<bool> MatchAsync(MatchContext context)
    {
        if (_expectedContent == null && _assertion == null)
        {
            return true;
        }

        if (context.Request.Content != null && _assertion != null)
        {
            try
            {
                return _assertion(context.Request.Content);
            }
            catch
            {
                return false;
            }
        }
        
        if (context.Request.Content != null)
        {
            var requestContent = await context.GetRequestContentAsync();

            return string.Equals(_expectedContent, requestContent);
        }
        
        return false;
    }

    public bool Match(string? expectedContent)
    {
        return string.Equals(_expectedContent, expectedContent);
    }
}