using UnityEngine;
using UnityEngine.UI;
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

        [Header("Dependencies")]
        [SerializeField] private NetworkDespawnerSceneLoader despawnerSceneLoader;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            escMenuPanel.SetActive(false);

            if (resumeButton != null)
                resumeButton.onClick.AddListener(Toggle);

            if (leaveGameButton != null)
                leaveGameButton.onClick.AddListener(OnLeaveGameClicked);

            if (quitGameButton != null)
                quitGameButton.onClick.AddListener(OnQuitGameClicked);
        }

        public void Toggle()
        {
            PlayerState playerState = FindObjectOfType<PlayerState>();

            if (!escMenuPanel.activeSelf && playerState != null && playerState.IsMatchFrozen())
                return;

            if (escMenuPanel.activeSelf)
            {
                escMenuPanel.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                playerState?.SetPlayerMovementState(PlayerMovementState.Idling);
            }
            else
            {
                escMenuPanel.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                playerState?.SetPlayerMovementState(PlayerMovementState.InShop);
            }
        }

        private void OnLeaveGameClicked()
        {
            despawnerSceneLoader.LoadNetworkDespawnerSceneForEveryone();
        }

        private void OnQuitGameClicked()
        {
            Application.Quit();
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
    }
}