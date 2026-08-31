using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Assemblies.ClientOrchestratorBridge
{
    /// <summary>
    /// Waits for the single auth ticket response that belongs to one issued handle, out of the
    /// stream of responses Steam broadcasts to every listener.
    /// </summary>
    internal sealed class SteamAuthTicketResponseWaiter
    {
        /// <summary>
        /// Continuations must never run inline: a response is delivered from inside Steam's native
        /// callback dispatch, and resuming the awaiting caller there would run its cleanup (and
        /// anything it awaits next) inside that dispatch.
        /// </summary>
        private readonly TaskCompletionSource<SteamAuthTicketResponse> _responseForIssuedHandle =
            new TaskCompletionSource<SteamAuthTicketResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// Steam may deliver the response from inside the request call itself, before the caller has
        /// been told which handle it was issued, so early responses are held until the handle is known.
        /// </summary>
        private readonly List<SteamAuthTicketResponse> _responsesDeliveredBeforeTheHandleWasKnown =
            new List<SteamAuthTicketResponse>();

        private readonly object _stateGate = new object();

        private IDisposable _responseSubscription;
        private uint? _issuedAuthTicketHandle;

        /// <summary>
        /// Hands the waiter the subscription that feeds it, so it can unsubscribe the instant the
        /// response it was waiting for arrives. The caller stays responsible for disposing the
        /// subscription on paths where no response ever arrives; disposal is idempotent.
        /// </summary>
        public void AttachResponseSubscription(IDisposable responseSubscription)
        {
            lock (_stateGate)
                _responseSubscription = responseSubscription;
        }

        /// <summary>
        /// Records the handle Steam issued and replays any response that arrived before it was known.
        /// </summary>
        public void AdoptIssuedAuthTicketHandle(uint issuedAuthTicketHandle)
        {
            SteamAuthTicketResponse? responseForIssuedHandle = null;

            lock (_stateGate)
            {
                _issuedAuthTicketHandle = issuedAuthTicketHandle;

                foreach (var earlyResponse in _responsesDeliveredBeforeTheHandleWasKnown)
                {
                    if (earlyResponse.AuthTicketHandle != issuedAuthTicketHandle)
                        continue;

                    responseForIssuedHandle = earlyResponse;
                    break;
                }

                _responsesDeliveredBeforeTheHandleWasKnown.Clear();
            }

            if (responseForIssuedHandle.HasValue)
                CompleteWithResponseForIssuedHandle(responseForIssuedHandle.Value);
        }

        /// <summary>
        /// Receives every response Steam broadcasts, including responses for handles issued by other
        /// in-flight requests, which are ignored so their owners can still see them.
        /// </summary>
        public void OnAuthTicketResponseDelivered(SteamAuthTicketResponse response)
        {
            lock (_stateGate)
            {
                if (_issuedAuthTicketHandle == null)
                {
                    _responsesDeliveredBeforeTheHandleWasKnown.Add(response);
                    return;
                }

                if (response.AuthTicketHandle != _issuedAuthTicketHandle.Value)
                    return;
            }

            CompleteWithResponseForIssuedHandle(response);
        }

        /// <exception cref="TimeoutException">
        /// No response for the issued handle arrived within <paramref name="responseTimeout"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
        public async Task<SteamAuthTicketResponse> WaitForResponseForIssuedHandle(
            TimeSpan responseTimeout,
            CancellationToken cancellationToken
        )
        {
            using (var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                var timeoutTask = Task.Delay(responseTimeout, timeoutCancellation.Token);
                var firstCompletedTask = await Task
                    .WhenAny(_responseForIssuedHandle.Task, timeoutTask)
                    .ConfigureAwait(false);

                if (firstCompletedTask != timeoutTask)
                {
                    // Releases the timer rather than leaving it pending for the whole timeout.
                    timeoutCancellation.Cancel();
                    return await _responseForIssuedHandle.Task.ConfigureAwait(false);
                }

                cancellationToken.ThrowIfCancellationRequested();

                throw new TimeoutException(
                    $"Steam did not deliver a web API auth ticket response within {responseTimeout}."
                );
            }
        }

        private void CompleteWithResponseForIssuedHandle(SteamAuthTicketResponse response)
        {
            IDisposable responseSubscriptionToDispose;

            lock (_stateGate)
                responseSubscriptionToDispose = _responseSubscription;

            // Unsubscribing before completing keeps every later response - including a duplicate for
            // our own handle - away from a waiter that is already done.
            responseSubscriptionToDispose?.Dispose();

            // A second response for the same handle must be tolerated rather than throwing.
            _responseForIssuedHandle.TrySetResult(response);
        }
    }
}
