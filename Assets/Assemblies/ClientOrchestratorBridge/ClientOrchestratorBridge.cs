using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Assemblies.ClientOrchestratorBridge;
using Resonance.Assemblies.LobbySystem;
using Resonance.Contracts;

namespace Resonance.Assemblies.ClientOrchestratorBridge
{
    public class ClientOrchestratorBridge
    {
        private const string SteamAuthIdentityString = "dev.bchen.ResonanceServerOrchestrator";
        private readonly Platform _platform;
        private readonly HttpClient _client;
        private readonly IPlatformUserResolver _userResolver;

        public ClientOrchestratorBridge(
            HttpClient client,  // intentionally no default to allow for both URL customization and DI
            IPlatformUserResolver platformUserResolver,
            Platform platform
        )
        {
            _client = client;
            _userResolver = platformUserResolver;
            _platform = platform;
        }

        public async Task<JoinMatchDto> GetJoinMatchDtoForLobby(
            Lobby lobby
        )
        {
            var platformUserId = _userResolver.GetPlatformId();

            var ticket = await _userResolver.GetAuthTicketForIdentityString(SteamAuthIdentityString);
            var platformUserInformation = new PlatformUserInformationDto(
                platform: _platform,
                platformUserId: platformUserId,
                platformLobbyId: lobby.LobbyId,
                authenticationTicketHex: ticket
            );

            var expectedLobbyPlayers = GetExpectedLobbyPlayerDtosForLobby(lobby);

            return new JoinMatchDto(
                platformUserInformation: platformUserInformation,
                expectedLobbyPlayers: expectedLobbyPlayers.ToArray()
            );
        }

        private List<ExpectedLobbyPlayerDto> GetExpectedLobbyPlayerDtosForLobby(Lobby lobby)
        {
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

        public async Task<JoinMatchResultDto> JoinMatch(
            JoinMatchDto joinMatchDto
        )
        {
            throw new System.NotImplementedException();
        }

        public async void LeaveMatch(
            LeaveMatchDto leaveMatchDto
        )
        {
            // on success, returns 204
        }

        public static ClientOrchestratorBridge BuildWithPlatform(Platform platform, HttpClient client)
        {
            throw new System.NotImplementedException();
        }
    }
}