using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;
    public HttpRequestMessage? LastRequest { get; private set; }
    public int CallCount { get; private set; }

    /// <remarks>
    /// The token <see cref="HttpClient"/> hands the handler, which is a source linked to the
    /// caller's token rather than the caller's token itself. Assert on its cancellation state, not
    /// on its identity.
    /// </remarks>
    public CancellationToken LastObservedCancellationToken { get; private set; }

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : this((request, _) => Task.FromResult(responder(request)))
    {
    }

    /// <remarks>
    /// Takes the token so a test can model a request the orchestrator parks: return a task that
    /// only settles when the test says so, or when the token is cancelled.
    /// </remarks>
    public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
    {
        _responder = responder;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastObservedCancellationToken = cancellationToken;
        CallCount++;
        return _responder(request, cancellationToken);
    }
}
