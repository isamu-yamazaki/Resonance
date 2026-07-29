using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
    public HttpRequestMessage? LastRequest { get; private set; }
    public int CallCount { get; private set; }

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        CallCount++;
        return Task.FromResult(_responder(request));
    }
}