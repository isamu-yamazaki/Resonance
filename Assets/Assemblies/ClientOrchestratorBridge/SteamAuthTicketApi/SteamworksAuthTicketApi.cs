using System;
using Steamworks;

namespace Assemblies.ClientOrchestratorBridge
{
    /// <summary>
    /// The real <see cref="ISteamAuthTicketApi"/>: a translation layer over the Steamworks statics,
    /// holding no state of its own so the surrounding state machine stays Steamworks-free.
    /// </summary>
    /// <remarks>
    /// Steamworks only dispatches callbacks while something pumps <see cref="SteamAPI.RunCallbacks"/>.
    /// The only pump in this project lives in SteamLobbyProvider
    /// (Assets/Scripts/Lobby/Providers/SteamLobbyProvider.cs, RunSteamCallbacks), so an auth ticket
    /// requested while no SteamLobbyProvider is pumping will never see its response and will fail with
    /// a <see cref="TimeoutException"/>. This adapter cannot fix that; the pump has to outlive the request.
    /// </remarks>
    public class SteamworksAuthTicketApi : ISteamAuthTicketApi
    {
        public uint RequestWebApiAuthTicket(string identityString)
        {
            return SteamUser.GetAuthTicketForWebApi(identityString).m_HAuthTicket;
        }

        public IDisposable SubscribeToAuthTicketResponses(Action<SteamAuthTicketResponse> onAuthTicketResponse)
        {
            if (onAuthTicketResponse == null)
                throw new ArgumentNullException(nameof(onAuthTicketResponse));

            return new SteamworksAuthTicketResponseSubscription(
                steamworksResponse => onAuthTicketResponse(ToSteamAuthTicketResponse(steamworksResponse))
            );
        }

        public void CancelAuthTicket(uint authTicketHandle)
        {
            SteamUser.CancelAuthTicket(new HAuthTicket(authTicketHandle));
        }

        /// <summary>
        /// Steam always marshals the full fixed-size ticket buffer, so the buffer is carried across
        /// untouched together with the length that says how much of it is meaningful.
        /// </summary>
        private static SteamAuthTicketResponse ToSteamAuthTicketResponse(
            GetTicketForWebApiResponse_t steamworksResponse
        )
        {
            return new SteamAuthTicketResponse(
                steamworksResponse.m_hAuthTicket.m_HAuthTicket,
                (int)steamworksResponse.m_eResult,
                steamworksResponse.m_rgubTicket,
                steamworksResponse.m_cubTicket
            );
        }
    }
}
