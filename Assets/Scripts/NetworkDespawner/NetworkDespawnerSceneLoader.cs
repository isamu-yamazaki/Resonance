using PurrNet;
using UnityEngine;

namespace Resonance.NetworkDespawner
{

    public class NetworkDespawnerSceneLoader : NetworkBehaviour
    {
        public void LoadNetworkDespawnerSceneForEveryone()
        {
            if (isServer)
            {
                networkManager.sceneModule.LoadSceneAsync("NetworkDespawnerScene");
            } else
            {
                Debug.Log("[NetworkDespawnerSceneLoader] Can only load next scene from server");
            }
        }
    }
}
