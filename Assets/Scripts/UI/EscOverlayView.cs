using UnityEngine;
using UnityEngine.UI;
using Resonance.NetworkDespawner;
using Resonance.Assemblies.UISystem;
using UnityEngine.Events;

namespace Resonance.UI
{
    public class EscOverlayView : MonoBehaviour, IOverlayView
    {
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

        private UnityAction resumeCallback = null;

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
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (resumeButton != null)
            {
                resumeCallback = () => viewActions.Dismiss();
                resumeButton.onClick.AddListener(resumeCallback);
            }
        }

        public void OnHide()
        {
            quitConfirmationPanel.SetActive(false);

            escMenuPanel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (resumeCallback != null)
            {
                resumeButton.onClick.RemoveListener(resumeCallback);
                resumeCallback = null;
            }
        }

        private void OnLeaveGameClicked()
        {
            despawnerSceneLoader.LoadNetworkDespawnerSceneForEveryone();
        }

        private void OnQuitGameClicked()
        {
            quitConfirmationPanel.SetActive(true);
        }

        private void OnQuitConfirmed()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void OnQuitCancelled()
        {
            quitConfirmationPanel.SetActive(false);
        }

    }
}
