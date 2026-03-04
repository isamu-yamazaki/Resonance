using System.Threading.Tasks;
using PurrNet;
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

        private async void DestroyNetworkManager()
        {
            if (networkManager.isClientOnly)
            {
                await Task.Delay(1000);
                networkManager.StopClient();
                Destroy(networkManager.gameObject);
                LoadLobbyScene();
            }
            else
            {
                while (networkManager.playerCount >= 2)
                {
                    await Task.Delay(1000);
                }

                networkManager.StopServer();
                Destroy(networkManager.gameObject);
                LoadLobbyScene();
            }
        }

        private void LoadLobbyScene()
        {
            SceneManager.LoadScene("LobbyScene");
        }
    }
}
