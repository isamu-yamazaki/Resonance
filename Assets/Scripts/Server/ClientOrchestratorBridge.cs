using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Resonance.Assemblies.LobbySystem;
using Resonance.BuildTools;
using UnityEngine;

namespace Resonance.Server
{
    /// <summary>
    /// Interactor between the client and the orchestrator.
    /// Join and leave the match at the appropriate time with the correct metadata.
    /// </summary>
    public class ClientOrchestratorBridge : MonoBehaviour
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

        public void JoinMatchFromCurrentLobby()
        {
            if (_lobbyDataHolder.CurrentLobby.IsValid)
            {
                _ = CreateAndSendCurrentLobbyAsync();
            }
        }

        public void LeaveMatchFromCurrentLobby()
        {
            // TODO: implement
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
