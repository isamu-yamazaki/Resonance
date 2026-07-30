using System;
using System.Threading.Tasks;
using Steamworks;

namespace Assemblies.ClientOrchestratorBridge
{
    /// <summary>
    /// Resolves the local Steam user for the orchestrator. The auth ticket flow itself lives in
    /// <see cref="SteamAuthTicketRequester"/>; this type only supplies the Steamworks-backed
    /// implementation of that flow's dependencies.
    /// </summary>
    public class SteamPlatformUserResolver : IPlatformUserResolver
    {
        private readonly SteamAuthTicketRequester _authTicketRequester;

        public SteamPlatformUserResolver()
            : this(new SteamworksAuthTicketApi(), SteamAuthTicketRequester.DefaultTicketResponseTimeout)
        {
        }

        public SteamPlatformUserResolver(ISteamAuthTicketApi steamAuthTicketApi)
            : this(steamAuthTicketApi, SteamAuthTicketRequester.DefaultTicketResponseTimeout)
        {
        }

        public SteamPlatformUserResolver(ISteamAuthTicketApi steamAuthTicketApi, TimeSpan ticketResponseTimeout)
        {
            _authTicketRequester = new SteamAuthTicketRequester(steamAuthTicketApi, ticketResponseTimeout);
        }

        public string GetPlatformId()
        {
            return SteamUser.GetSteamID().ToString();
        }

        public Task<string> GetAuthTicketForIdentityString(string identityString)
        {
            return _authTicketRequester.RequestAuthTicketHexForIdentityString(identityString);
        }
    }
}
