using PurrNet;
using Resonance.Assemblies.LobbySystem;
using Resonance.BuildTools;
using UnityEngine;

namespace Resonance.GameBootstrap
{
    public class NextSceneLoader : NetworkBehaviour
    {
        /// <summary>
        /// Perform a networked load into the next scene using the environment variables.
        /// </summary>
        [ServerOnly]
        public void LoadNextScene()
        {
            var sceneToSwitchTo = EnvironmentVariablesReceiver.Instance?.NextSceneName;
            if (sceneToSwitchTo == null)
            {
                Debug.LogError($"Unable to find {nameof(EnvironmentVariablesReceiver)} component; scene switching will not work.");
                return;
            }
            networkManager.sceneModule.LoadSceneAsync(sceneToSwitchTo);
        }
    }
}
