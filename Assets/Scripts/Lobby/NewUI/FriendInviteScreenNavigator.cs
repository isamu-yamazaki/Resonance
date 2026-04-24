using UnityEngine;

namespace Resonance.LobbySystem.NewUI
{
    public class FriendInviteScreenNavigator : MonoBehaviour
    {
        [SerializeField] private LobbyManager lobbyManager;
        [SerializeField] private LobbyViewRouterBridge viewRouterBridge;

        private void Start()
        {
            if (lobbyManager != null)
            {
                lobbyManager.OnFriendInviteAccepted.AddListener(OnInviteAccepted);
            }
        }

        private void OnDestroy()
        {
            if (lobbyManager != null)
            {
                lobbyManager.OnFriendInviteAccepted.RemoveListener(OnInviteAccepted);
            }
        }

        private void OnInviteAccepted(string lobbyId)
        {
            if (viewRouterBridge == null || viewRouterBridge.viewRouter == null)
            {
                return;
            }

            viewRouterBridge.viewRouter.PushScreenView(RoomScreenView.Key);
        }
    }
}
