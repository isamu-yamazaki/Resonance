using System;
using Steamworks;

namespace Assemblies.ClientOrchestratorBridge
{
    /// <summary>
    /// Keeps a <see cref="Callback{T}"/> alive for as long as someone is listening for auth ticket
    /// responses, and unregisters it on disposal - the only way a Steamworks callback is ever
    /// unregistered. Disposal is idempotent and safe to perform from inside the callback itself,
    /// because the dispatcher iterates a copy of its callback list while dispatching.
    /// </summary>
    internal sealed class SteamworksAuthTicketResponseSubscription : IDisposable
    {
        private readonly Callback<GetTicketForWebApiResponse_t> _responseCallback;

        private bool _isDisposed;

        public SteamworksAuthTicketResponseSubscription(
            Callback<GetTicketForWebApiResponse_t>.DispatchDelegate onSteamworksResponse
        )
        {
            _responseCallback = Callback<GetTicketForWebApiResponse_t>.Create(onSteamworksResponse);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _responseCallback.Dispose();
        }
    }
}
