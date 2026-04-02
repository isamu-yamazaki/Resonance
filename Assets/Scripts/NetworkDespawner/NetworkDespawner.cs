using System.Threading.Tasks;
using PurrNet;
using Resonance.BuildTools;
using Resonance.Match;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Resonance.NetworkDespawner
{
    public class NetworkDespawner : MonoBehaviour
    {
        private NetworkManager networkManager;

        private void Awake()
        {
            // in case the user gets sent here randomly
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            networkManager = FindFirstObjectByType<NetworkManager>();

            Debug.Log("[NetworkDespawner] Despawning network objects");

            networkManager.ResetOriginalScene(SceneManager.GetActiveScene());

            DestroyMatchLogic();
            DestroyNetworkManager();
        }

        // protected override void OnSpawned()
        // {
        //     base.OnSpawned();

        //     Debug.Log("[NetworkDespawner] Despawning network objects");

        //     networkManager.ResetOriginalScene(SceneManager.GetActiveScene());

        //     DestroyMatchLogic();
        //     DestroyNetworkManager();
        // }

        private void DestroyMatchLogic()
        {
            if (MatchLogicNetworkAdapter.Instance != null)
            {
                Debug.Log("[NetworkDespawner] Destroying match logic");
                MatchLogicNetworkAdapter.Instance.Despawn();
            }
            else
            {
                Debug.Log("[NetworkDespawner] Match logic already destroyed");
            }
        }

        private bool HasServerConfig => ServerBuildConfigReceiver.Instance != null;

        private async void DestroyNetworkManager()
        {
            if (!networkManager.isOffline)
            {
                if (networkManager.isClientOnly)
                {
                    await Task.Delay(1000);
                    networkManager.StopClient();
                    Destroy(networkManager.gameObject);
                    LoadLobbyScene();
                    return;
                }
                else if (HasServerConfig)
                {
                    while (networkManager.playerCount >= 1)
                    {
                        await Task.Delay(1000);
                    }
                }
                else
                {
                    while (networkManager.playerCount >= 2)
                    {
                        await Task.Delay(1000);
                    }
                }
            }

            networkManager.StopServer();
            Destroy(networkManager.gameObject);

            if (HasServerConfig)
                QuitApplication();
            else
                LoadLobbyScene();
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
