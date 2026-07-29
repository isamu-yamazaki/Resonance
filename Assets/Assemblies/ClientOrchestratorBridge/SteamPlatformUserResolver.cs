using System;
using System.Threading.Tasks;
using Steamworks;

namespace Assemblies.ClientOrchestratorBridge
{
    public class SteamPlatformUserResolver : IPlatformUserResolver
    {
        public string GetPlatformId()
        {
            return SteamUser.GetSteamID().ToString();
        }

        public Task<string> GetAuthTicketForIdentityString(string identityString)
        {
            var tcs = new TaskCompletionSource<string>();

            Callback<GetTicketForWebApiResponse_t> callback = null;
            var ticketHandle = SteamUser.GetAuthTicketForWebApi(identityString);
            callback = Callback<GetTicketForWebApiResponse_t>.Create(result =>
            {
                if (result.m_hAuthTicket != ticketHandle)
                    return;

                if (result.m_eResult != EResult.k_EResultOK)
                    return;

                // Get the ticket (range of unsigned bytes) as a hex string
                string hexTicket = BitConverter.ToString(result.m_rgubTicket, 0, result.m_cubTicket)
                    .Replace("-", "");
                tcs.SetResult(hexTicket);

                callback?.Dispose();
            });

            return tcs.Task;
        }
    }
}