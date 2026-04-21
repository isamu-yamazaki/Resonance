using Resonance.Assemblies.LobbySystem;
using Resonance.Assemblies.UISystem;
using Resonance.LobbySystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.LobbySystem.NewUI
{
    public class CreateJoinScreenView : MonoBehaviour, IScreenView
    {
        public static string Key => nameof(CreateJoinScreenView);
        string IScreenView.Key => Key;

        [SerializeField] private Button createButton;
        [SerializeField] private TMP_InputField codeInput;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button backButton;
        [SerializeField] private LobbyManager lobbyManager;

        private ScreenViewActions _viewActions;

        private void Start()
        {
            createButton.onClick.AddListener(OnCreateClicked);
            joinButton.onClick.AddListener(OnJoinClicked);
            backButton.onClick.AddListener(OnBackClicked);
        }

        private void OnDestroy()
        {
            UnsubscribeLobbyEvents();
        }

        public void OnShow(ScreenViewActions viewActions)
        {
            _viewActions = viewActions;
            gameObject.SetActive(true);
            SubscribeLobbyEvents();
        }

        public void OnHide()
        {
            gameObject.SetActive(false);
            UnsubscribeLobbyEvents();
            _viewActions = default;
        }

        private void SubscribeLobbyEvents()
        {
            if (lobbyManager == null) return;
            lobbyManager.OnRoomJoined.AddListener(OnRoomJoined);
            lobbyManager.OnRoomJoinFailed.AddListener(OnRoomJoinFailed);
        }

        private void UnsubscribeLobbyEvents()
        {
            if (lobbyManager == null) return;
            lobbyManager.OnRoomJoined.RemoveListener(OnRoomJoined);
            lobbyManager.OnRoomJoinFailed.RemoveListener(OnRoomJoinFailed);
        }

        private void OnCreateClicked()
        {
            lobbyManager.CreateRoom();
        }

        private void OnJoinClicked()
        {
            lobbyManager.JoinLobby(codeInput.text);
        }

        private void OnBackClicked()
        {
            _viewActions.Back?.Invoke();
        }

        private void OnRoomJoined(Lobby lobby)
        {
            _viewActions.ShowScreen?.Invoke(RoomScreenView.Key);
        }

        private void OnRoomJoinFailed(string error)
        {
            Debug.LogWarning($"[CreateJoinScreenView] Room join failed: {error}");
        }
    }
}
