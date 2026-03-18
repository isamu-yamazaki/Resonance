using PurrNet;
using Resonance.LobbySystem;
using UnityEngine;

namespace Resonance.GameBootstrap
{
    public class NextSceneLoader : NetworkBehaviour
    {
        // [SerializeField] private NetworkManager networkManager;

        private LobbyDataHolder lobbyDataHolder;


        protected override void OnSpawned()
        {
            base.OnSpawned();

            lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
            if (!lobbyDataHolder)
            {
                Debug.LogError($"Unable to find {nameof(LobbyDataHolder)} component; scene switching will not work.");
            }
        }

        public void LoadNextScene()
        {
            var sceneToSwitchTo = lobbyDataHolder.CurrentLobby.SceneName;
            networkManager.sceneModule.LoadSceneAsync(sceneToSwitchTo);
        }
    }
}
