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
        }

        private void OnPlayClicked() =>
            _viewActions.ShowScreen?.Invoke(CreateJoinScreenView.Key);

        private void OnFriendsOverlayClicked() =>
            _viewActions.ShowOverlay?.Invoke(FriendOverlayView.Key);

        private void OnSkinSelectClicked() =>
            _viewActions.ShowScreen?.Invoke(SkinScreenView.Key);
    }
}
