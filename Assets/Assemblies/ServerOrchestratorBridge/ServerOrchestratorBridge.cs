using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Resonance.Assemblies.ClientOrchestratorBridge;
using Resonance.Assemblies.OrchestratorHelpers;
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

        private string SignalReadyEndpointPath => $"v1/server/{_matchId}/ready";
        private const string PathSegmentSeparator = "/";

        private const string CancelledWhileAwaitingOrchestratorMessage =
            "The caller cancelled before the orchestrator answered.";

        private const string MatchKeyHeader = "X-Match-Key";

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
            _matchId = matchId;
            _matchKey = matchKey;
            InjectMatchKey();
        }

        #region Server requests

        public async Task SignalAsReady(
            CancellationToken cancellationToken = default
        )
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                BuildEndpointUri(_client.BaseAddress, SignalReadyEndpointPath)
            );

            try
            {
                using var response = await _client.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                var responseBody = response.Content != null
                    ? await response.Content.ReadAsStringAsync()
                    : string.Empty;

                throw new OrchestratorRequestException(
                    $"The orchestrator answered {(int)response.StatusCode} ({response.StatusCode}) instead of the result its endpoint promises.",
                    response.StatusCode,
                    responseBody
                );
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (OperationCanceledException cancellation)
            {
                throw new TaskCanceledException(CancelledWhileAwaitingOrchestratorMessage, cancellation);
            }
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
            _client.DefaultRequestHeaders.Add(MatchKeyHeader, _matchKey);
        }


        /// <remarks>
        /// Resolving against a base address with no trailing slash would drop its last path segment,
        /// so an orchestrator hosted under a path prefix would lose the prefix.
        /// </remarks>
        private static Uri BuildEndpointUri(Uri baseAddress, string endpointPath)
        {
            var baseAddressAsDirectory = baseAddress.AbsoluteUri.EndsWith(PathSegmentSeparator)
                ? baseAddress
                : new Uri(baseAddress.AbsoluteUri + PathSegmentSeparator);

            return new Uri(baseAddressAsDirectory, endpointPath);
        }

        #endregion
    }
}