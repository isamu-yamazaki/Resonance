using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Resonance.Assemblies.LobbySystem;
using UnityEngine;

namespace Resonance.Server
{
    /// <summary>
    /// Editor/testing utility. Creates a mock lobby and POSTs it to the orchestrator server
    /// so that <see cref="ServerLobbyCodeReader"/> can fetch it during testing.
    /// Attach to any GameObject in the server start scene and trigger via the context menu
    /// or a UI Button's OnClick event.
    /// </summary>
    public class MockLobbyCreator : MonoBehaviour
    {
        [SerializeField] private string orchestratorUrl = "http://localhost:9000";
        [SerializeField] private string lobbyId = "ABCD";
        [SerializeField] private string lobbyName = "Test Lobby";
        [SerializeField] private int maxPlayers = 4;
        [SerializeField] private GameMode gameMode = GameMode.Arena;
        [SerializeField] private string sceneName = "GameBootstrapScene";

#if UNITY_EDITOR
        [ContextMenu("Create And Send Mock Lobby")]
        public void CreateAndSendMockLobby()
        {
            _ = CreateAndSendMockLobbyAsync();
        }
#endif

        private async Task CreateAndSendMockLobbyAsync()
        {
            var properties = new Dictionary<string, string>
        {
            { LobbyMetadataKeys.GameMode,  gameMode.ToString() },
            { LobbyMetadataKeys.SceneName, sceneName },
        };

            Lobby lobby = LobbyFactory.Create(lobbyName, lobbyId, maxPlayers,
                                              new List<LobbyUser>(), properties);
            string json = lobby.ToJson();

            try
            {
                using var client = new HttpClient();
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{orchestratorUrl}/lobbies/{lobbyId}", content);

                if (response.IsSuccessStatusCode)
                {
                    Debug.Log($"[MockLobbyCreator] Lobby '{lobbyId}' sent successfully.");
                }
                else
                {
                    Debug.LogError($"[MockLobbyCreator] Failed to send lobby: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MockLobbyCreator] Error sending lobby: {ex.Message}");
            }
        }
    }

}
