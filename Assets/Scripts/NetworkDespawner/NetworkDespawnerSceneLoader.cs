using PurrNet;

namespace Resonance.NetworkDespawner
{
    public class NetworkDespawnerSceneLoader : NetworkBehaviour
    {
        public void LoadNetworkDespawnerSceneForEveryone()
        {
            LoadNetworkDespawnerScene_Server();
        }

        [ServerRpc]
        private void LoadNetworkDespawnerScene_Server()
        {
            networkManager.sceneModule.LoadSceneAsync("NetworkDespawnerScene");
        }
    }
}
