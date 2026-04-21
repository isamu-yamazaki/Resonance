using Resonance.Assemblies.UISystem;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEditor.Overlays;
using UnityEngine;

namespace Resonance.LobbySystem.NewUI
{
    public class LobbyViewRouterBridge : MonoBehaviour
    {
        public static LobbyViewRouterBridge Instance { get; private set; }

        public ViewRouter viewRouter { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
            }
            Instance = this;

            viewRouter = new ViewRouter();
            RegisterViews();
        }

        private void RegisterViews()
        {
            var lobbyPanel = GetComponentInChildren<LobbyPanelScreenView>(includeInactive: true);
            if (lobbyPanel == null)
            {
                Debug.LogError("[LobbyViewRouterBridge] LobbyPanelScreenView not found in children.");
                return;
            }

            var createJoinView = GetComponentInChildren<CreateJoinScreenView>(includeInactive: true);
            if (createJoinView == null)
            {
                Debug.LogError("[LobbyViewRouterBridge] CreateJoinScreenView not found in children.");
                return;
            }

            var roomView = GetComponentInChildren<RoomScreenView>(includeInactive: true);
            if (roomView == null)
            {
                Debug.LogError("[LobbyViewRouterBridge] RoomScreenView not found in children.");
                return;
            }

            var skinView = GetComponentInChildren<SkinScreenView>(includeInactive: true);
            if (skinView == null)
            {
                Debug.LogError("[LobbyViewRouterBridge] SkinScreenView not found in children.");
                return;
            }

            var friendOverlayView = GetComponentInChildren<FriendOverlayView>(includeInactive: true);
            if (friendOverlayView == null)
            {
                Debug.LogError("[LobbyViewRouterBridge] FriendOverlayView not found in children.");
                return;
            }
            var lobbySettingsOverlayView = GetComponentInChildren<LobbySettingsOverlayView>(includeInactive: true);
            if (lobbySettingsOverlayView == null)
            {
                Debug.LogError("[LobbyViewRouterBridge] LobbySettingsOverlayView not found in children.");
                return;
            }


            viewRouter.RegisterScreenView(new ScreenViewOptions
            {
                view = lobbyPanel,
                unlockCursorWhenShown = true
            });
            viewRouter.RegisterScreenView(new ScreenViewOptions
            {
                view = createJoinView,
                unlockCursorWhenShown = true
            });
            viewRouter.RegisterScreenView(new ScreenViewOptions
            {
                view = roomView,
                unlockCursorWhenShown = true
            });
            viewRouter.RegisterScreenView(new ScreenViewOptions
            {
                view = skinView,
                unlockCursorWhenShown = true
            });
            viewRouter.RegisterOverlay(new OverlayOptions
            {
                view = friendOverlayView,
                unlockCursorWhenShown = true
            });
            viewRouter.RegisterOverlay(new OverlayOptions
            {
                view = lobbySettingsOverlayView,
                unlockCursorWhenShown = true
            });

            viewRouter.PushScreenView(LobbyPanelScreenView.Key);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
