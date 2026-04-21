using UnityEngine;
using UnityEngine.UI;
using Resonance.NetworkDespawner;
using Resonance.Assemblies.UISystem;
using System;

namespace Resonance.UI
{
    public class EscOverlayView : MonoBehaviour, IOverlayView
    {
        public static string Key => nameof(EscOverlayView);
        string IOverlayView.Key => Key;

        public static EscOverlayView Instance { get; private set; }

        [Header("ESC Menu UI")]
        [SerializeField] private GameObject escMenuPanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button leaveGameButton;
        [SerializeField] private Button quitGameButton;

        [Header("Quit Confirmation")]
        [SerializeField] private GameObject quitConfirmationPanel;
        [SerializeField] private Button quitYesButton;
        [SerializeField] private Button quitNoButton;

        [Header("Dependencies")]
        [SerializeField] private NetworkDespawnerSceneLoader despawnerSceneLoader;

#if !UNITY_SERVER
        [Header("Wwise Events")]
        [SerializeField] private AK.Wwise.Event buttonClickEvent;
        [SerializeField] private AK.Wwise.Event resumeClickEvent;
        [SerializeField] private AK.Wwise.Event leaveClickEvent;
#endif

        private Action dismiss;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (despawnerSceneLoader == null)
                despawnerSceneLoader = FindFirstObjectByType<NetworkDespawnerSceneLoader>();

            escMenuPanel.SetActive(false);
            quitConfirmationPanel.SetActive(false);

            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeClicked);

            if (leaveGameButton != null)
                leaveGameButton.onClick.AddListener(OnLeaveGameClicked);

            if (quitGameButton != null)
                quitGameButton.onClick.AddListener(OnQuitGameClicked);

            if (quitYesButton != null)
                quitYesButton.onClick.AddListener(OnQuitConfirmed);

            if (quitNoButton != null)
                quitNoButton.onClick.AddListener(OnQuitCancelled);
        }

        public void OnShow(OverlayViewActions viewActions)
        {
            quitConfirmationPanel.SetActive(false);
            escMenuPanel.SetActive(true);
            dismiss = viewActions.Dismiss;
        }

        public void OnHide()
        {
            quitConfirmationPanel.SetActive(false);
            escMenuPanel.SetActive(false);
            dismiss = null;
        }

#if !UNITY_SERVER
        private void PostClick(AK.Wwise.Event wwiseEvent)
        {
            if (wwiseEvent != null && wwiseEvent.IsValid())
                wwiseEvent.Post(gameObject);
        }
#endif

        private void OnResumeClicked()
        {
#if !UNITY_SERVER
            PostClick(resumeClickEvent);
#endif
            dismiss?.Invoke();
        }

        private void OnLeaveGameClicked()
        {
#if !UNITY_SERVER
            PostClick(leaveClickEvent);
#endif
            despawnerSceneLoader.LoadNetworkDespawnerSceneForEveryone();
        }

        private void OnQuitGameClicked()
        {
#if !UNITY_SERVER
            PostClick(buttonClickEvent);
#endif
            quitConfirmationPanel.SetActive(true);
        }

        private void OnQuitConfirmed()
        {
#if !UNITY_SERVER
            PostClick(buttonClickEvent);
#endif
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void OnQuitCancelled()
        {
#if !UNITY_SERVER
            PostClick(buttonClickEvent);
#endif
            quitConfirmationPanel.SetActive(false);
        }
    }
}
