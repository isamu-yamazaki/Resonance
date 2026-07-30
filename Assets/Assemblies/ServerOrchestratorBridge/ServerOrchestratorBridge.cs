using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Resonance.Contracts;

namespace Resonance.Assemblies.ServerOrchestratorBridge
{
    /// <summary>
    /// Interact with the orchestrator's server endpoints.
    /// Unlike <see cref="ClientOrchestratorBridge"/>, the class is fully platform-agnostic.
    /// </summary>
    public class ServerOrchestratorBridge
    {
        private readonly HttpClient _client;
        private readonly string _matchId;
        private readonly string _matchKey;

        /// <param name="client">The HTTP client containing the base address.</param>
        /// <param name="matchId">The match ID in the orchestrator.</param>
        /// <param name="matchKey">The secure match key provided by the orchestrator.</param>
        public ServerOrchestratorBridge(
            HttpClient client,
            string matchId, // ServerOrchestratorBridge only lives as long as the match itself
            string matchKey
        )
        {
            _client = client;
            _matchId =  matchId;
            _matchKey = matchKey;
            InjectMatchKey();
        }

        #region Server requests

        public async Task SignalAsReady()
        {
            throw new System.NotImplementedException();
        }

        public async Task<List<MatchMemberDto>> GetMembers(
        )
        {
            throw new System.NotImplementedException();
        }

        #endregion

        #region Helpers

        private void InjectMatchKey()
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
}