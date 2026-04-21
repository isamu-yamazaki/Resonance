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

        private void Awake()
        {

        }

        public void OnHide()
        {

        }

        public void OnShow(ScreenViewActions viewActions)
        {

        }
    }
}
