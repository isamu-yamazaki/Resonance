using System;
using System.Collections.Generic;
using System.Linq;
using Resonance.Assemblies.UISystem;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Resonance.LobbySystem.NewUI
{
    public class FriendOverlayView : MonoBehaviour, IOverlayView, IPointerClickHandler
    {
        public static string Key => nameof(FriendOverlayView);
        string IOverlayView.Key => Key;

        [SerializeField] private LobbyManager lobbyManager;
        [SerializeField] private GameObject friendEntryPrefab;
        [SerializeField] private GameObject listEmptyMessage;
        [SerializeField] private Transform friendsListContent;
        [SerializeField] private LobbyManager.FriendFilter filter = LobbyManager.FriendFilter.Online;

        private readonly Dictionary<string, EntryBinding> _entries = new Dictionary<string, EntryBinding>();
        private float _lastPullTime;
        private Action _dismiss;
        private bool _isShown;

        public void OnShow(OverlayViewActions viewActions)
        {
            _dismiss = viewActions.Dismiss;
            gameObject.SetActive(true);
            lobbyManager.OnFriendListPulled.AddListener(HandleFriendListPulled);
            lobbyManager.PullFriends(filter);
            _lastPullTime = Time.time;
            _isShown = true;
        }

        public void OnHide()
        {
            lobbyManager.OnFriendListPulled.RemoveListener(HandleFriendListPulled);
            ClearEntries();
            _dismiss = null;
            _isShown = false;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_isShown)
            {
                return;
            }

            if (_lastPullTime + 3f < Time.time)
            {
                _lastPullTime = Time.time;
                lobbyManager.PullFriends(filter);
            }

            foreach (var entry in _entries.Values)
            {
                if (!entry.Button.interactable && entry.InviteTime + 3f < Time.time)
                {
                    entry.Button.interactable = true;
                }
            }
        }

        private void HandleFriendListPulled(List<FriendUser> friends)
        {
            var newIds = new HashSet<string>(friends.Select(f => f.Id));
            var staleIds = _entries.Keys.Where(id => !newIds.Contains(id)).ToList();
            foreach (var id in staleIds)
            {
                Destroy(_entries[id].Root);
                _entries.Remove(id);
            }

            foreach (var friend in friends)
            {
                if (_entries.ContainsKey(friend.Id))
                {
                    continue;
                }
                _entries[friend.Id] = CreateEntry(friend);
            }

            if (_entries.Count == 0 && listEmptyMessage != null)
            {
                listEmptyMessage.SetActive(true);
            } else if (listEmptyMessage != null)
            {
                listEmptyMessage.SetActive(false);
            }
        }

        private EntryBinding CreateEntry(FriendUser friend)
        {
            var go = Instantiate(friendEntryPrefab, friendsListContent);
            var nameText = go.transform.Find("Name").GetComponent<TMP_Text>();
            var avatar = go.transform.Find("Avatar").GetComponent<RawImage>();
            var button = go.GetComponent<Button>();

            nameText.text = friend.DisplayName;
            avatar.texture = friend.Avatar;

            var binding = new EntryBinding { Root = go, Button = button, InviteTime = -999f };
            var captured = friend;
            button.onClick.AddListener(() => OnInviteClicked(captured, binding));
            return binding;
        }

        private void OnInviteClicked(FriendUser friend, EntryBinding binding)
        {
            if (!binding.Button.interactable)
            {
                return;
            }
            binding.InviteTime = Time.time;
            lobbyManager.InviteFriend(friend);
            binding.Button.interactable = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.pointerPressRaycast.gameObject != gameObject)
            {
                return;
            }
            _dismiss?.Invoke();
        }

        private void ClearEntries()
        {
            foreach (var entry in _entries.Values)
            {
                Destroy(entry.Root);
            }
            _entries.Clear();
        }

        private class EntryBinding
        {
            public GameObject Root;
            public Button Button;
            public float InviteTime;
        }
    }
}
