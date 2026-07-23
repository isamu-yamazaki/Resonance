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
        private static readonly HttpClient Client = new();
        private ClientBuildConfigReceiver _clientBuildConfigReceiver;
        private LobbyDataHolder _lobbyDataHolder;
        private ClientBuildConfig BuildConfig => _clientBuildConfigReceiver.Config;

        private void Awake()
        {
            _clientBuildConfigReceiver = FindFirstObjectByType<ClientBuildConfigReceiver>();
            _lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
        }

        public void CreateAndSendCurrentLobbyIfClientModeAndLobbyHost()
        {
            if (_lobbyDataHolder.CurrentLobby.IsOwner(_lobbyDataHolder.LocalUserId))
            {
                _ = CreateAndSendCurrentLobbyAsync();
            }
        }

        private async Task CreateAndSendCurrentLobbyAsync()
        {
            Lobby lobby = _lobbyDataHolder.CurrentLobby;
            string json = lobby.ToJson();

            try
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await Client.PostAsync($"{BuildConfig.orchestratorUrl}/lobbies/{lobby.LobbyCode}", content);

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
