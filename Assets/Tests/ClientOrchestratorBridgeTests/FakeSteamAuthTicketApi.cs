using System;
using System.Collections.Generic;
using Assemblies.ClientOrchestratorBridge;

/// <summary>
/// Hand-driven stand-in for Steam's auth ticket API. Responses are delivered synchronously to every
/// live subscription, exactly like Steamworks callbacks, and a live-subscription counter exposes
/// whether the code under test cleaned up after itself.
/// </summary>
public class FakeSteamAuthTicketApi : ISteamAuthTicketApi
{
    public const uint DefaultIssuedAuthTicketHandle = 1u;

    private readonly List<Action<SteamAuthTicketResponse>> _liveSubscriptions = new List<Action<SteamAuthTicketResponse>>();
    private readonly List<string> _requestedIdentityStrings = new List<string>();
    private readonly List<uint> _cancelledAuthTicketHandles = new List<uint>();
    private readonly Queue<uint> _authTicketHandlesToIssue = new Queue<uint>();

    /// <summary>Handle returned once the queue seeded by <see cref="EnqueueAuthTicketHandlesToIssue"/> runs dry.</summary>
    public uint AuthTicketHandleToIssue { get; set; } = DefaultIssuedAuthTicketHandle;

    /// <summary>
    /// Invoked from inside <see cref="RequestWebApiAuthTicket"/> with the handle just issued, so a test
    /// can deliver a response before the request call has returned.
    /// </summary>
    public Action<uint> RespondSynchronouslyDuringTicketRequest { get; set; }

    public IReadOnlyList<string> RequestedIdentityStrings => _requestedIdentityStrings;
    public IReadOnlyList<uint> CancelledAuthTicketHandles => _cancelledAuthTicketHandles;
    public int TicketRequestCount => _requestedIdentityStrings.Count;
    public int LiveSubscriptionCount => _liveSubscriptions.Count;
    public int TotalSubscriptionCount { get; private set; }

    public void EnqueueAuthTicketHandlesToIssue(params uint[] authTicketHandles)
    {
        foreach (var authTicketHandle in authTicketHandles)
            _authTicketHandlesToIssue.Enqueue(authTicketHandle);
    }

    public uint RequestWebApiAuthTicket(string identityString)
    {
        _requestedIdentityStrings.Add(identityString);

        var issuedAuthTicketHandle = _authTicketHandlesToIssue.Count > 0
            ? _authTicketHandlesToIssue.Dequeue()
            : AuthTicketHandleToIssue;

        RespondSynchronouslyDuringTicketRequest?.Invoke(issuedAuthTicketHandle);

        return issuedAuthTicketHandle;
    }

    public IDisposable SubscribeToAuthTicketResponses(Action<SteamAuthTicketResponse> onAuthTicketResponse)
    {
        if (onAuthTicketResponse == null)
            throw new ArgumentNullException(nameof(onAuthTicketResponse));

        _liveSubscriptions.Add(onAuthTicketResponse);
        TotalSubscriptionCount++;

        return new AuthTicketResponseSubscription(this, onAuthTicketResponse);
    }

    public void CancelAuthTicket(uint authTicketHandle)
    {
        _cancelledAuthTicketHandles.Add(authTicketHandle);
    }

    /// <summary>
    /// Delivers a response to every live subscription, mirroring Steam's behaviour of broadcasting
    /// each response to all registered callbacks regardless of which handle they care about.
    /// Iterates a snapshot so a subscriber may dispose itself while being invoked.
    /// </summary>
    public void DeliverResponseToEveryLiveSubscription(SteamAuthTicketResponse response)
    {
        foreach (var subscription in _liveSubscriptions.ToArray())
            subscription.Invoke(response);
    }

    private void RemoveSubscription(Action<SteamAuthTicketResponse> onAuthTicketResponse)
    {
        _liveSubscriptions.Remove(onAuthTicketResponse);
    }

    private class AuthTicketResponseSubscription : IDisposable
    {
        private readonly FakeSteamAuthTicketApi _owner;
        private readonly Action<SteamAuthTicketResponse> _onAuthTicketResponse;
        private bool _isDisposed;

        public AuthTicketResponseSubscription(
            FakeSteamAuthTicketApi owner,
            Action<SteamAuthTicketResponse> onAuthTicketResponse
        )
        {
            _owner = owner;
            _onAuthTicketResponse = onAuthTicketResponse;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _owner.RemoveSubscription(_onAuthTicketResponse);
        }
    }
}
