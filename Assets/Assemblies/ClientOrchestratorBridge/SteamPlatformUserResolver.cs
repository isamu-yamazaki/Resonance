using Steamworks;

namespace Assemblies.ClientOrchestratorBridge
{
    public class SteamPlatformUserResolver : IPlatformUserResolver
    {
        public string GetPlatformId()
        {
           return SteamUser.GetSteamID().ToString();
        }

        public string GetAuthTicketForIdentityString(string identityString)
        {
            var ticket = SteamUser.GetAuthTicketForWebApi(identityString);
            return ticket.ToString();
        }
    }
}