using PurrNet;

namespace Resonance.NetworkDespawner
{

    public class NetworkDespawnerSceneLoader : NetworkBehaviour
    {
        public void LoadNetworkDespawnerSceneForEveryone()
        {
            networkManager.sceneModule.LoadSceneAsync("NetworkDespawnerScene");
        }
    }
}
