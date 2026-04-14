using UnityEngine;
using UnityEngine.UI;
using Resonance.Helper;
using Resonance.NetworkDespawner;
using Resonance.PlayerController;

namespace Resonance.UI
{
    public class EscMenuManager : MonoBehaviour
    {
        public static EscMenuManager Instance { get; private set; }

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

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (despawnerSceneLoader == null)
                despawnerSceneLoader = FindObjectOfType<NetworkDespawnerSceneLoader>();
            
            escMenuPanel.SetActive(false);
            quitConfirmationPanel.SetActive(false);

            if (resumeButton != null)
                resumeButton.onClick.AddListener(Toggle);

            if (leaveGameButton != null)
                leaveGameButton.onClick.AddListener(OnLeaveGameClicked);

            if (quitGameButton != null)
                quitGameButton.onClick.AddListener(OnQuitGameClicked);

            if (quitYesButton != null)
                quitYesButton.onClick.AddListener(OnQuitConfirmed);

            if (quitNoButton != null)
                quitNoButton.onClick.AddListener(OnQuitCancelled);
        }

        public void Toggle()
        {
            PlayerState playerState = OwnerFinder.FindFirstOwnedObjectByType<PlayerState>();

            if (!escMenuPanel.activeSelf && playerState != null && playerState.IsMatchFrozen())
                return;

            // Close confirmation panel too if it was open
            quitConfirmationPanel.SetActive(false);

            if (escMenuPanel.activeSelf)
            {
                escMenuPanel.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                escMenuPanel.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
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
