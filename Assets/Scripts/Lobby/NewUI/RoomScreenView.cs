using System;
using System.Collections;
using System.Collections.Generic;
using Resonance.Assemblies.LobbySystem;
using Resonance.Assemblies.UISystem;
using Resonance.LobbySystem.TemporaryUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.LobbySystem.NewUI
{
    public class RoomScreenView : MonoBehaviour, IScreenView
    {
        public static string Key => nameof(RoomScreenView);
        string IScreenView.Key => Key;

        [Header("Map Options")]
        [SerializeField] private string[] mapOptions = { "NightCity", "TB_PlaytestArena" };

        [Header("Top Bar")]
        [SerializeField] private Button leaveButton;
        [SerializeField] private Button friendsButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button skinSelectButton;

        [Header("Room Code")]
        [SerializeField] private Button copyCodeButton;
        [SerializeField] private TMP_Text copyCodeText;

        [Header("Options")]
        [SerializeField] private TMP_Dropdown gameModeDropdown;
        [SerializeField] private TMP_Dropdown mapDropdown;

        [Header("Member List")]
        [SerializeField] private MemberEntry memberEntryPrefab;
        [SerializeField] private Transform memberListContent;

        [Header("Ready")]
        [SerializeField] private Button readyButton;

        [Header("Skin Preview")]
        [SerializeField] private SkinPreviewModel skinPreviewModel;
        [SerializeField] private RawImage skinPreviewImage;

        [Header("Dependencies")]
        [SerializeField] private LobbyManager lobbyManager;

#if !UNITY_SERVER
        [Header("Wwise Events")]
        [SerializeField] private AK.Wwise.Event buttonClickEvent;
        [SerializeField] private AK.Wwise.Event readyClickEvent;
        [SerializeField] private AK.Wwise.Event leaveClickEvent;
        [SerializeField] private AK.Wwise.Event copyCodeClickEvent;
#endif

        private ScreenViewActions _viewActions;
        private bool _isApplyingLobbyUpdate;
        private bool _dropdownsPopulated;
        private Coroutine _copyEffectCoroutine;

        private static readonly WaitForSeconds CopyTypeDelay = new WaitForSeconds(0.13f);
        private static readonly WaitForSeconds CopyReturnDelay = new WaitForSeconds(1f);

        private void Start()
        {
            leaveButton.onClick.AddListener(OnLeaveClicked);
            friendsButton.onClick.AddListener(OnFriendsClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
            skinSelectButton.onClick.AddListener(OnSkinSelectClicked);
            copyCodeButton.onClick.AddListener(OnCopyCodeClicked);
            readyButton.onClick.AddListener(OnReadyClicked);

            gameModeDropdown.onValueChanged.AddListener(OnGameModeDropdownChanged);
            mapDropdown.onValueChanged.AddListener(OnMapDropdownChanged);
        }

        public void OnShow(ScreenViewActions viewActions)
        {
            _viewActions = viewActions;
            gameObject.SetActive(true);

            if (skinPreviewModel != null && skinPreviewImage != null)
            {
                skinPreviewImage.texture = skinPreviewModel.PreviewTexture;
            }

            PopulateDropdowns();
            SubscribeLobbyEvents();

            var lobby = lobbyManager.CurrentLobby;
            ApplyLobbyState(lobby);
            RefreshMembers(lobby);
        }

        public void OnHide()
        {
            UnsubscribeLobbyEvents();
            gameObject.SetActive(false);
            _viewActions = default;
        }

        private void OnDestroy()
        {
            UnsubscribeLobbyEvents();
        }

        private void SubscribeLobbyEvents()
        {
            if (lobbyManager == null) return;
            lobbyManager.OnRoomUpdated.AddListener(OnRoomUpdated);
            lobbyManager.OnRoomLeft.AddListener(OnRoomLeft);
        }

        private void UnsubscribeLobbyEvents()
        {
            if (lobbyManager == null) return;
            lobbyManager.OnRoomUpdated.RemoveListener(OnRoomUpdated);
            lobbyManager.OnRoomLeft.RemoveListener(OnRoomLeft);
        }

#if !UNITY_SERVER
        private void PostClick(AK.Wwise.Event wwiseEvent)
        {
            if (wwiseEvent != null && wwiseEvent.IsValid())
                wwiseEvent.Post(gameObject);
        }
#endif

        private void PopulateDropdowns()
        {
            if (_dropdownsPopulated) return;

            _isApplyingLobbyUpdate = true;

            gameModeDropdown.ClearOptions();
            gameModeDropdown.AddOptions(new List<string>(Enum.GetNames(typeof(GameMode))));

            mapDropdown.ClearOptions();
            mapDropdown.AddOptions(new List<string>(mapOptions));

            _isApplyingLobbyUpdate = false;
            _dropdownsPopulated = true;
        }

        private void OnRoomUpdated(Lobby lobby)
        {
            ApplyLobbyState(lobby);
            RefreshMembers(lobby);
        }

        private void OnRoomLeft()
        {
            ClearMembers();
            _viewActions.Back?.Invoke();
        }

        private void ApplyLobbyState(Lobby lobby)
        {
            if (!lobby.IsValid) return;

            copyCodeText.text = "Copy room code";

            _isApplyingLobbyUpdate = true;

            gameModeDropdown.value = (int)lobby.GameMode;

            var mapIndex = Array.IndexOf(mapOptions, lobby.SceneName);
            mapDropdown.value = mapIndex >= 0 ? mapIndex : 0;

            _isApplyingLobbyUpdate = false;
        }

        private void RefreshMembers(Lobby lobby)
        {
            if (!lobby.IsValid) return;

            UpdateExistingMembers(lobby);
            AddNewMembers(lobby);
            RemoveLeftMembers(lobby);
        }

        private void UpdateExistingMembers(Lobby lobby)
        {
            foreach (Transform child in memberListContent)
            {
                if (!child.TryGetComponent(out MemberEntry member)) continue;

                var matchingMember = lobby.Members.Find(x => x.Id == member.MemberId);
                if (!string.IsNullOrEmpty(matchingMember.Id))
                    member.SetReady(matchingMember.IsReady);
            }
        }

        private void AddNewMembers(Lobby lobby)
        {
            var existingMembers = memberListContent.GetComponentsInChildren<MemberEntry>(includeInactive: true);

            foreach (var member in lobby.Members)
            {
                if (Array.Exists(existingMembers, x => x.MemberId == member.Id)) continue;

                var entry = Instantiate(memberEntryPrefab, memberListContent);
                entry.Init(member);
            }
        }

        private void RemoveLeftMembers(Lobby lobby)
        {
            var toRemove = new List<Transform>();

            for (int i = 0; i < memberListContent.childCount; i++)
            {
                var child = memberListContent.GetChild(i);
                if (!child.TryGetComponent(out MemberEntry member)) continue;

                if (!lobby.Members.Exists(x => x.Id == member.MemberId))
                    toRemove.Add(child);
            }

            foreach (var child in toRemove)
                Destroy(child.gameObject);
        }

        private void ClearMembers()
        {
            foreach (Transform child in memberListContent)
                Destroy(child.gameObject);
        }

        private void OnLeaveClicked()
        {
#if !UNITY_SERVER
            PostClick(leaveClickEvent);
#endif
            lobbyManager.LeaveLobby();
        }

        private void OnFriendsClicked()
        {
#if !UNITY_SERVER
            PostClick(buttonClickEvent);
#endif
            _viewActions.ShowOverlay?.Invoke(FriendOverlayView.Key);
        }

        private void OnSettingsClicked()
        {
#if !UNITY_SERVER
            PostClick(buttonClickEvent);
#endif
            _viewActions.ShowOverlay?.Invoke(LobbySettingsOverlayView.Key);
        }

        private void OnSkinSelectClicked()
        {
#if !UNITY_SERVER
            PostClick(buttonClickEvent);
#endif
            _viewActions.ShowScreen?.Invoke(SkinScreenView.Key);
        }

        private void OnReadyClicked()
        {
#if !UNITY_SERVER
            PostClick(readyClickEvent);
#endif
            lobbyManager.ToggleLocalReady();
        }

        private void OnCopyCodeClicked()
        {
            var lobby = lobbyManager.CurrentLobby;
            if (!lobby.IsValid || string.IsNullOrEmpty(lobby.LobbyCode)) return;

#if !UNITY_SERVER
            PostClick(copyCodeClickEvent);
#endif
            GUIUtility.systemCopyBuffer = lobby.LobbyCode;

            if (_copyEffectCoroutine != null)
                StopCoroutine(_copyEffectCoroutine);
            _copyEffectCoroutine = StartCoroutine(CopyCodeEffect());
        }

        private IEnumerator CopyCodeEffect()
        {
            copyCodeText.text = "Copied!";
            yield return CopyReturnDelay;

            copyCodeText.text = "Copy room code";
            _copyEffectCoroutine = null;
        }

        private void OnGameModeDropdownChanged(int index)
        {
            if (_isApplyingLobbyUpdate) return;
#if !UNITY_SERVER
            PostClick(buttonClickEvent);
#endif
            lobbyManager.SetGameModeOnLobby((GameMode)index);
        }

        private void OnMapDropdownChanged(int index)
        {
            if (_isApplyingLobbyUpdate) return;
            if (index < 0 || index >= mapOptions.Length) return;
#if !UNITY_SERVER
            PostClick(buttonClickEvent);
#endif
            lobbyManager.SetSceneNameOnLobby(mapOptions[index]);
        }
    }
}
