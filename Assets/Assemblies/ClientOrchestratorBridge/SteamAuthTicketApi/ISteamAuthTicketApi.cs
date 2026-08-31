using System;

namespace Assemblies.ClientOrchestratorBridge
{
    /// <summary>
    /// Seam over the handful of Steamworks calls needed to obtain a web API auth ticket.
    /// Deliberately expresses handles, result codes and ticket bytes as plain types so that
    /// consumers stay compilable (and testable) under DISABLESTEAMWORKS.
    /// </summary>
    public interface ISteamAuthTicketApi
    {
        /// <summary>
        /// Asks Steam to issue a web API auth ticket for the given identity string.
        /// Returns <see cref="SteamAuthTicketRequester.InvalidAuthTicketHandle"/> when the request
        /// could not be issued, in which case no response will ever be delivered.
        /// </summary>
        uint RequestWebApiAuthTicket(string identityString);

        /// <summary>
        /// Registers a listener that receives every auth ticket response Steam delivers, including
        /// responses for handles issued by other in-flight requests. Disposing the returned handle
        /// is the only way to unregister the listener, and must be safe to do more than once and
        /// from inside the listener itself.
        /// </summary>
        IDisposable SubscribeToAuthTicketResponses(Action<SteamAuthTicketResponse> onAuthTicketResponse);

        /// <summary>
        /// Releases an auth ticket that will not be validated by the orchestrator.
        /// </summary>
        void CancelAuthTicket(uint authTicketHandle);
    }
}
