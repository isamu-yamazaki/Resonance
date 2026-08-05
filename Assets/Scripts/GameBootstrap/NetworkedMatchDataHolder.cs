using System.Collections.Generic;
using System.Threading.Tasks;
using PurrNet;
using Resonance.Contracts;

namespace Resonance.GameBootstrap
{
    /// <summary>
    /// Holds the match data for the current match.
    /// </summary>
    public class NetworkedMatchDataHolder : NetworkBehaviour
    {
        /// <summary>
        /// Server-side information about the members.
        /// IMPORTANT: it contains the connection token required to connect to the server.
        /// Never expose directly to clients.
        /// </summary>
        private List<MatchMemberDto> _serverMembers = new();

        [ServerOnly]
        public void SetMembers(List<MatchMemberDto> members)
        {
            _serverMembers = members;
        }

        [ServerOnly]
        public PlayerIdentity? ExchangePlayerIdentityForClientToken(string clientToken)
        {
            foreach (var member in _serverMembers)
            {
                if (member.ServerAuthToken == clientToken)
                {
                    return new PlayerIdentity(member.Platform, member.PlatformUserId);
                }
            }

            return null;
        }

        [ServerRpc]
        private async Task<string> GetDisplayName(PlayerIdentity identity)
        {
            foreach (var member in _serverMembers)
            {
                if (member.PlatformUserId == identity.PlatformUserId && member.Platform == identity.Platform)
                {
                    return member.Username;
                }
            }

            return null;
        }
    }
}
