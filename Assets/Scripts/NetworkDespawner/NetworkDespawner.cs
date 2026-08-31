using System;
using System.Net.Http;
using System.Threading.Tasks;
using PurrNet;
using Resonance.Assemblies.ClientOrchestratorBridge;
using Resonance.Assemblies.LobbySystem;
using Resonance.BuildTools;
using Resonance.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Resonance.NetworkDespawner
{
    public class NetworkDespawner : MonoBehaviour
    {
        private NetworkManager _networkManager;
        private LobbyDataHolder _lobbyDataHolder;

        private void Awake()
        {
            // in case the user gets sent here randomly
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            _networkManager = FindFirstObjectByType<NetworkManager>();
            _lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();

            Debug.Log("[NetworkDespawner] Despawning network objects");

            _networkManager.ResetOriginalScene(SceneManager.GetActiveScene());

            _ = DestroyNetworkManager();
        }

        private bool HasServerConfig => ServerBuildConfigReceiver.Instance != null;

        private async Task DestroyNetworkManager()
        {
            if (!_networkManager.isOffline)
            {
                if (_networkManager.isClientOnly)
                {
                    await Task.Delay(1000);
                    _networkManager.StopClient();
                    Destroy(_networkManager.gameObject);
                    await AttemptLeaveOrchestrator();
                    LoadLobbyScene();
                    return;
                }

                if (HasServerConfig)
                {
                    while (_networkManager.playerCount >= 1)
                    {
                        await Task.Delay(1000);
                    }
                }
                else
                {
                    while (_networkManager.playerCount >= 2)
                    {
                        await Task.Delay(1000);
                    }
                }
            }

            _networkManager.StopServer();
            Destroy(_networkManager.gameObject);

            if (HasServerConfig)
            {
                QuitApplication();
            }
            else
            {
                LoadLobbyScene();
            }
        }

        private async Task AttemptLeaveOrchestrator()
        {
            Debug.Log("[NetworkDespawner] Disconnecting from orchestrator");

            var config = ClientBuildConfigReceiver.Instance?.Config;
            if (config == null) return;

            if (_lobbyDataHolder == null) return;

            var client = new HttpClient();
            client.BaseAddress = new Uri(config.orchestratorUrl);
            var bridge = ClientOrchestratorBridge.BuildWithPlatform(
                config.enableSteamLobby ? Platform.Steam : Platform.Dummy,
                client
            );
            try
            {
                var leaveMatchDto =
                    await bridge.GetLeaveMatchDtoForLobby(_lobbyDataHolder.CurrentLobby, _lobbyDataHolder.LocalUserId);
                await bridge.LeaveMatch(leaveMatchDto);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(NetworkDespawner)}] Failed to leave the orchestrator match: {e}");
            }
        }

        private void LoadLobbyScene()
        {
            SceneManager.LoadScene("LobbyScene");
        }

        private void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
