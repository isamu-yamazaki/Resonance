using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Resonance.Assemblies.LobbySystem;
using Resonance.BuildTools;
using Resonance.LobbySystem;
using UnityEngine;

namespace Resonance.Server
{
    /// <summary>
    /// Editor/testing utility. Creates a mock lobby and POSTs it to the orchestrator server
    /// so that <see cref="ServerLobbyCodeReader"/> can fetch it during testing.
    /// Attach to any GameObject in the server start scene and trigger via the context menu
    /// or a UI Button's OnClick event.
    /// </summary>
    public class ServerOrchestratorLobbyCreator : MonoBehaviour
    {
        private ClientBuildConfigReceiver clientBuildConfigReceiver;
        private LobbyDataHolder lobbyDataHolder;
        private ClientBuildConfig buildConfig => clientBuildConfigReceiver.Config;

        private void Awake()
        {
            clientBuildConfigReceiver = FindFirstObjectByType<ClientBuildConfigReceiver>();
            lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
        }

        public void CreateAndSendCurrentLobbyIfClientModeAndLobbyHost()
        {
            if (buildConfig.useClientServerMode && lobbyDataHolder.CurrentLobby.IsOwner(lobbyDataHolder.LocalUserId))
            {
                _ = CreateAndSendCurrentLobbyAsync();
            }
        }

        private async Task CreateAndSendCurrentLobbyAsync()
        {
            Lobby lobby = lobbyDataHolder.CurrentLobby;
            string json = lobby.ToJson();

            try
            {
                using var client = new HttpClient();
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{buildConfig.orchestratorUrl}/lobbies/{lobby.LobbyCode}", content);

                if (response.IsSuccessStatusCode)
                {
                    Debug.Log($"[ServerOrchestratorLobbyCreator] Lobby '{lobby.LobbyCode}' sent successfully.");
                }
                else
                {
                    Debug.LogError($"[ServerOrchestratorLobbyCreator] Failed to send lobby: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ServerOrchestratorLobbyCreator] Error sending lobby: {ex.Message}");
            }
        }
    }
}
