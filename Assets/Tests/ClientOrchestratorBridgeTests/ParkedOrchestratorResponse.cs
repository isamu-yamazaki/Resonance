using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Stands in for the orchestrator's long-lived join request, which parks for up to ~75 seconds
/// while the rest of the roster arrives. The response settles only when the test releases it or
/// when the cancellation token the handler was given is cancelled, so a test can assert on
/// mid-flight behaviour without waiting on real time.
/// </summary>
public sealed class ParkedOrchestratorResponse
{
    private readonly TaskCompletionSource<HttpResponseMessage> _parkedResponse =
        new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly TaskCompletionSource<bool> _requestReachedHandler =
        new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Completes once the request has actually been handed to the message handler, so a test never
    /// has to guess whether the send has started yet.
    /// </summary>
    public Task RequestReachedHandler => _requestReachedHandler.Task;

    /// <remarks>
    /// A real handler aborts the exchange when its token is cancelled; mirroring that here is what
    /// makes cancellation observable end to end.
    /// </remarks>
    public Task<HttpResponseMessage> ParkUntilReleasedOrCancelled(CancellationToken cancellationToken)
    {
        _requestReachedHandler.TrySetResult(true);
        cancellationToken.Register(() => _parkedResponse.TrySetCanceled(cancellationToken));
        return _parkedResponse.Task;
    }

    public void ReleaseWith(HttpResponseMessage response)
    {
        _parkedResponse.TrySetResult(response);
    }

    public void ReleaseAsCancelled()
    {
        _parkedResponse.TrySetCanceled();
    }
}
