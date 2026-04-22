using System.Threading.Tasks;
using Resonance.Assemblies.LobbySystem;
using UnityEngine;

namespace Resonance.LobbySystem.DataProviders
{
    public class DefaultSceneNameSetter : MonoBehaviour
    {
        [SerializeField] private LobbyManager lobbyManager;
        [SerializeField] private string sceneName = "TB_ArenaDemo";

        public void Start()
        {
            lobbyManager.OnRoomJoined.AddListener(OnRoomJoined);
        }

        private async void OnRoomJoined(Lobby arg0)
        {
            await Task.Delay(500);
            SetDefaultSceneName();
        }

        public void SetDefaultSceneName()
        {
            if (lobbyManager.CurrentLobby.IsOwner(lobbyManager.LocalUserId))
            {
                lobbyManager.SetSceneNameOnLobby(sceneName);
            }
        }
    }
}
