using Resonance.Assemblies.UISystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.LobbySystem.NewUI
{
    public class LobbyPanelScreenView : MonoBehaviour, IScreenView
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button friendsOverlayButton;
        [SerializeField] private Button skinSelectButton;
        [SerializeField] private Button settingsButton;

        [SerializeField] private TMP_Text username;
        [SerializeField] private Image userAvatar;

        [SerializeField] private LobbyManager lobbyManager;

        public static string Key => nameof(LobbyPanelScreenView);
        string IScreenView.Key => Key;

        private ScreenViewActions _viewActions;

        private void Start()
        {
            playButton.onClick.AddListener(OnPlayClicked);
            friendsOverlayButton.onClick.AddListener(OnFriendsOverlayClicked);
            skinSelectButton.onClick.AddListener(OnSkinSelectClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);

            if (lobbyManager != null)
            {
                lobbyManager.onInitialized.AddListener(RefreshLocalUser);
            }
        }

        private void OnDestroy()
        {
            if (lobbyManager != null)
            {
                lobbyManager.onInitialized.RemoveListener(RefreshLocalUser);
            }
        }

        public void OnHide()
        {
            gameObject.SetActive(false);
            _viewActions = default;
        }

        public void OnShow(ScreenViewActions viewActions)
        {
            _viewActions = viewActions;
            gameObject.SetActive(true);
            RefreshLocalUser();
        }

        private async void RefreshLocalUser()
        {
            if (lobbyManager == null || lobbyManager.CurrentProvider == null)
            {
                return;
            }

            var displayName = await lobbyManager.GetLocalDisplayName();
            if (username != null)
            {
                username.text = displayName ?? string.Empty;
            }

            var avatar = await lobbyManager.GetLocalAvatar();
            if (avatar != null && userAvatar != null)
            {
                userAvatar.sprite = Sprite.Create(
                    avatar,
                    new Rect(0f, 0f, avatar.width, avatar.height),
                    new Vector2(0.5f, 0.5f));
            }
        }

        private void OnPlayClicked() =>
            _viewActions.ShowScreen?.Invoke(CreateJoinScreenView.Key);

        private void OnFriendsOverlayClicked() =>
            _viewActions.ShowOverlay?.Invoke(FriendOverlayView.Key);

        private void OnSkinSelectClicked() =>
            _viewActions.ShowScreen?.Invoke(SkinScreenView.Key);

        private void OnSettingsClicked() =>
            _viewActions.ShowOverlay?.Invoke(LobbySettingsOverlayView.Key);
    }
}
