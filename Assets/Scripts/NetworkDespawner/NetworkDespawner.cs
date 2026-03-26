using System.Threading.Tasks;
using PurrNet;
using Resonance.BuildTools;
using Resonance.Match;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Resonance.NetworkDespawner
{
    public class NetworkDespawner : NetworkBehaviour
    {
        protected override void OnSpawned()
        {
            base.OnSpawned();

            Debug.Log("[NetworkDespawner] Despawning network objects");

            // TODO: set this to the "disconnected" scene within the game (bootstrap)
            networkManager.ResetOriginalScene(SceneManager.GetActiveScene());

            DestroyMatchLogic();
            DestroyNetworkManager();
        }

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
