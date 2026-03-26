using System;
using System.Net.Http;
using System.Threading.Tasks;
using Resonance.Assemblies.LobbySystem;
using Resonance.LobbySystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Resonance.Server
{
    /// <summary>
    /// Runs on the dedicated server. Reads the room code from the -lobbyCode CLI argument
    /// and the orchestrator URL from the -orchestratorUrl CLI argument. Fetches the lobby
    /// data from the orchestrator, stores it in <see cref="LobbyDataHolder"/>, then loads
    /// the game scene so <see cref="ConnectionBootstrapper"/> can start the network.
    /// </summary>
    public class ServerLobbyCodeReader : MonoBehaviour
    {
        [SerializeField] private string gameSceneName = "GameBootstrapScene";
        [SerializeField] private string editorRoomCode = "";
        [SerializeField] private string editorOrchestratorUrl = "http://localhost:9000";

        private LobbyDataHolder lobbyDataHolder;

        private void Awake()
        {
            lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
            if (lobbyDataHolder == null)
            {
                Debug.LogError("[ServerLobbyCodeReader] Unable to find LobbyDataHolder in scene.");
                Destroy(this);
                return;
            }

            string lobbyCode = null;
            string orchestratorUrl = null;
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-lobbyCode") lobbyCode = args[i + 1];
                if (args[i] == "-orchestratorUrl") orchestratorUrl = args[i + 1];
            }

            if (lobbyCode != null && orchestratorUrl != null)
            {
                _ = LoadLobbyAndStartGameAsync(lobbyCode, orchestratorUrl);
            }
            else
            {
                Debug.LogWarning("[ServerLobbyCodeReader] Missing -lobbyCode or -orchestratorUrl. Use the inspector button to load manually.");
            }
        }

        private async Task LoadLobbyAndStartGameAsync(string lobbyCode, string orchestratorUrl)
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync($"{orchestratorUrl}/lobbies/{lobbyCode}");

                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogError($"[ServerLobbyCodeReader] Failed to fetch lobby {lobbyCode}: {response.StatusCode}");
                    return;
                }

                string json = await response.Content.ReadAsStringAsync();
                Lobby lobby = Lobby.FromJson(json);
                lobbyDataHolder.SetCurrentLobby(lobby);

                Debug.Log($"[ServerLobbyCodeReader] Lobby data set. Loading scene: {gameSceneName}");
                SceneManager.LoadScene(gameSceneName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ServerLobbyCodeReader] Error fetching lobby: {ex.Message}");
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Load Scene With Editor Room Code")]
        private void LoadWithEditorRoomCode()
        {
            if (lobbyDataHolder == null)
            {
                lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
            }
            _ = LoadLobbyAndStartGameAsync(editorRoomCode, editorOrchestratorUrl);
        }
#endif
    }
}
