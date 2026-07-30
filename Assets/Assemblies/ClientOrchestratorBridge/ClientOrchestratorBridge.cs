using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Assemblies.ClientOrchestratorBridge;
using Newtonsoft.Json;
using Resonance.Assemblies.LobbySystem;
using Resonance.Contracts;

namespace Resonance.Assemblies.ClientOrchestratorBridge
{
    public class ClientOrchestratorBridge
    {
        private const string SteamAuthIdentityString = "dev.bchen.ResonanceServerOrchestrator";

        private const string JoinMatchEndpointPath = "v1/matches/join";
        private const string LeaveMatchEndpointPath = "v1/matches/leave";
        private const string JsonMediaType = "application/json";
        private const string PathSegmentSeparator = "/";

        private const string CancelledBeforeSendingMessage =
            "The caller cancelled before the request was sent to the orchestrator.";

        private const string CancelledWhileAwaitingOrchestratorMessage =
            "The caller cancelled before the orchestrator answered.";

        private readonly Platform _platform;
        private readonly HttpClient _client;
        private readonly IPlatformUserResolver _userResolver;

        /// <param name="client">
        /// Must carry the orchestrator address in <see cref="HttpClient.BaseAddress"/>.
        /// </param>
        /// <param name="platformUserResolver">
        /// Abstraction used to retrieve user information and authenticate the user
        /// on the platform.
        /// </param>
        /// <param name="platform">The platform the client is running on.</param>
        public ClientOrchestratorBridge(
            HttpClient client,
            IPlatformUserResolver platformUserResolver,
            Platform platform
        )
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (client.BaseAddress == null)
            {
                throw new ArgumentException(
                    $"{nameof(HttpClient)}.{nameof(HttpClient.BaseAddress)} must be set to the orchestrator address.",
                    nameof(client)
                );
            }

            _client = client;
            _userResolver = platformUserResolver ?? throw new ArgumentNullException(nameof(platformUserResolver));
            _platform = platform;
        }

        #region Converters

        public async Task<JoinMatchDto> GetJoinMatchDtoForLobby(
            Lobby lobby
        )
        {
            var expectedLobbyPlayers = GetExpectedLobbyPlayerDtosForLobby(lobby);

            var platformUserInformation = await GetPlatformUserInformationForLobby(lobby);

            return new JoinMatchDto(
                platformUserInformation: platformUserInformation,
                expectedLobbyPlayers: expectedLobbyPlayers.ToArray()
            );
        }

        public async Task<LeaveMatchDto> GetLeaveMatchDtoForLobby(
            Lobby lobby
        )
        {
            var platformUserInformation = await GetPlatformUserInformationForLobby(lobby);

            return new LeaveMatchDto(
                platformUserInformation: platformUserInformation
            );
        }

        #endregion

        private async Task<PlatformUserInformationDto> GetPlatformUserInformationForLobby(Lobby lobby)
        {
            var platformUserId = _userResolver.GetPlatformId();

            var ticket = await _userResolver.GetAuthTicketForIdentityString(SteamAuthIdentityString);

            return new PlatformUserInformationDto(
                platform: _platform,
                platformUserId: platformUserId,
                platformLobbyId: lobby.LobbyId,
                authenticationTicketHex: ticket
            );
        }

        /// <remarks>
        /// A null member list is refused rather than sent as an empty roster: the orchestrator would
        /// park waiting for a match that can never assemble.
        /// </remarks>
        private List<ExpectedLobbyPlayerDto> GetExpectedLobbyPlayerDtosForLobby(Lobby lobby)
        {
            if (lobby.Members == null)
            {
                throw new ArgumentException(
                    $"{nameof(Lobby)}.{nameof(Lobby.Members)} must list the roster the orchestrator should wait for.",
                    nameof(lobby)
                );
            }

            List<ExpectedLobbyPlayerDto> expectedLobbyPlayers = new List<ExpectedLobbyPlayerDto>();
            foreach (var lobbyMember in lobby.Members)
            {
                expectedLobbyPlayers.Add(new ExpectedLobbyPlayerDto(
                    platformUserId: lobbyMember.Id,
                    platform: _platform,
                    username: lobbyMember.DisplayName
                ));
            }

            return expectedLobbyPlayers;
        }

        #region Match requests

        /// <param name="joinMatchDto">The information required to join the match.</param>
        /// <param name="cancellationToken">
        /// A join is one long-lived request that the orchestrator parks until the whole roster
        /// arrives, so cancelling the HTTP request is what tells it this member is gone.
        /// </param>
        public async Task<JoinMatchResultDto> JoinMatch(
            JoinMatchDto joinMatchDto,
            CancellationToken cancellationToken = default
        )
        {
            if (joinMatchDto == null)
            {
                throw new ArgumentNullException(nameof(joinMatchDto));
            }

            ThrowIfCancelledBeforeSending(cancellationToken);

            using var response = await PostSerializedPayloadToEndpoint(
                JoinMatchEndpointPath,
                joinMatchDto,
                cancellationToken
            );

            var responseBody = await OrchestratorResponseInterpreter.ReadBodyOrEmpty(response);

            if (!response.IsSuccessStatusCode)
            {
                throw OrchestratorResponseInterpreter.InterpretUnsuccessfulJoinResponse(
                    response.StatusCode,
                    responseBody,
                    OrchestratorResponseInterpreter.ReadRetryAfterDelta(response)
                );
            }

            return OrchestratorResponseInterpreter.ReadJoinMatchResult(response.StatusCode, responseBody);
        }

        /// <remarks>
        /// A leave that lands on 404 has already happened: the orchestrator answers that way for
        /// anyone it is not currently tracking, which is the state the caller asked for.
        /// </remarks>
        public async Task LeaveMatch(
            LeaveMatchDto leaveMatchDto,
            CancellationToken cancellationToken = default
        )
        {
            if (leaveMatchDto == null)
            {
                throw new ArgumentNullException(nameof(leaveMatchDto));
            }

            ThrowIfCancelledBeforeSending(cancellationToken);

            // on success, returns 204
            using var response = await PostSerializedPayloadToEndpoint(
                LeaveMatchEndpointPath,
                leaveMatchDto,
                cancellationToken
            );

            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
            {
                return;
            }

            var responseBody = await OrchestratorResponseInterpreter.ReadBodyOrEmpty(response);

            throw OrchestratorResponseInterpreter.BuildUnexpectedResultOrchestratorRequestException(
                response.StatusCode,
                responseBody
            );
        }

        #endregion

        #region Sending

        /// <remarks>
        /// The payload is serialized with the serializer's defaults — PascalCase names and numeric
        /// enum values — because that is the request shape the deployed orchestrator accepts. How a
        /// response is read is a separate decision; see <see cref="OrchestratorResponseInterpreter"/>.
        /// <para>
        /// The request is disposed on every path, including the one where the send throws, so the
        /// serialized authentication ticket it carries does not outlive the exchange. Only the
        /// request is disposed here: the returned response, whose body the caller still has to read,
        /// belongs to the caller.
        /// </para>
        /// </remarks>
        private async Task<HttpResponseMessage> PostSerializedPayloadToEndpoint<TPayload>(
            string endpointPath,
            TPayload payload,
            CancellationToken cancellationToken
        )
        {
            var serializedPayload = JsonConvert.SerializeObject(payload);
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                BuildEndpointUri(_client.BaseAddress, endpointPath)
            );
            request.Content = new StringContent(serializedPayload, Encoding.UTF8, JsonMediaType);

            try
            {
                return await _client.SendAsync(request, cancellationToken);
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

        /// <remarks>
        /// Spelled as <see cref="TaskCanceledException"/> rather than
        /// <c>CancellationToken.ThrowIfCancellationRequested</c> so an already-cancelled caller sees
        /// the same exception type as one cancelled mid-flight.
        /// </remarks>
        private static void ThrowIfCancelledBeforeSending(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException(CancelledBeforeSendingMessage);
            }
        }

        #endregion

        public static ClientOrchestratorBridge BuildWithPlatform(Platform platform, HttpClient client)
        {
            IPlatformUserResolver platformUserResolver = platform switch
            {
                Platform.Steam => new SteamPlatformUserResolver(),
                Platform.Dummy => new DummyPlatformUserResolver(),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(platform),
                    platform,
                    $"No {nameof(IPlatformUserResolver)} exists for this platform."
                )
            };

            return new ClientOrchestratorBridge(client, platformUserResolver, platform);
        }
    }
}