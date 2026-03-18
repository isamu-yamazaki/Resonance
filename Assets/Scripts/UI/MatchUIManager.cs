using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Resonance.Match;
using Resonance.Assemblies.MatchStat;
using PurrNet;
using Resonance.NetworkDespawner;

namespace Resonance.UI
{
    public class MatchUIManager : NetworkBehaviour
    {
        [Header("HUD Elements")]
        [SerializeField] private TextMeshProUGUI kdaText;
        [SerializeField] private TextMeshProUGUI killStreakText;
        [SerializeField] private TextMeshProUGUI eliminationsText;

        [Header("Match End UI")]
        [SerializeField] private GameObject matchEndPanel;
        [SerializeField] private TextMeshProUGUI winnerText;
        [SerializeField] private TextMeshProUGUI finalStatsText;
        [SerializeField] private TextMeshProUGUI waitingForHostText;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button returnToLobbyButton;
        [SerializeField] private NetworkDespawnerSceneLoader despawnerSceneLoader;
        [SerializeField] private GameObject shopGameObject;  // disabled on match end

        [Header("Settings")]
        [SerializeField] private GameObject playerObject; // Assign the player to track
        [SerializeField] private bool showKillStreak = true;

        private void Start()
        {
            if (matchEndPanel != null)
            {
                matchEndPanel.SetActive(false);
            }

            // Initialize kill streak text to show 0
            if (killStreakText != null && showKillStreak)
            {
                killStreakText.text = "Kill Streak: 0";
                killStreakText.gameObject.SetActive(true);
            }

            SetupButtons();
            SubscribeToEvents();
            UpdateHUD();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            UpdateHUD();
        }

        #region Event Subscriptions
        private void SubscribeToEvents()
        {
            if (ArenaRoundManagerBridge.Instance != null)
            {
                ArenaRoundManagerBridge.Instance.OnMatchEnd += OnMatchEnd;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (ArenaRoundManagerBridge.Instance != null)
            {
                ArenaRoundManagerBridge.Instance.OnMatchEnd -= OnMatchEnd;
            }
        }
        #endregion

        #region Button Setup
        private void SetupButtons()
        {
            if (playAgainButton != null)
            {
                playAgainButton.onClick.AddListener(OnPlayAgainClicked);
                Debug.Log("[MatchUI] Play Again button listener added");
            }
            else
            {
                Debug.LogWarning("[MatchUI] Play Again button is null!");
            }

            if (returnToLobbyButton != null)
            {
                returnToLobbyButton.onClick.AddListener(OnReturnToLobbyClicked);
                Debug.Log("[MatchUI] Quit button listener added");
            }
            else
            {
                Debug.LogWarning("[MatchUI] Quit button is null!");
            }
        }

        private void OnPlayAgainClicked()
        {
            Debug.Log("[MatchUI] Play Again clicked!");

            // Reset time scale in case it was paused
            Time.timeScale = 1f;

            if (ArenaRoundManagerBridge.Instance != null)
            {
                ArenaRoundManagerBridge.Instance.StartMatchCountdown();
            }
        }

        private void OnReturnToLobbyClicked()
        {
            Debug.Log("[MatchUI] Return to lobby clicked!");
            despawnerSceneLoader.LoadNetworkDespawnerSceneForEveryone();
        }
        #endregion

        #region HUD Updates
        private async void UpdateHUD()
        {
            if (playerObject == null || MatchStatBridge.Instance == null) return;

            PlayerMatchStats? stats = await MatchStatBridge.Instance.GetStats(playerObject);
            if (stats == null) return;

            // Update KDA
            if (kdaText != null)
            {
                kdaText.text = $"K/D/A: {stats?.kills}/{stats?.deaths}/{stats?.assists} | KDA: {stats?.KDA:F2}";
            }

            // Update kill streak
            if (killStreakText != null && showKillStreak)
            {
                killStreakText.text = $"Kill Streak: {stats?.killStreak}";
                killStreakText.gameObject.SetActive(true);
            }

            // Update eliminations progress
            if (eliminationsText != null && ArenaRoundManagerBridge.Instance != null)
            {
                int target = ArenaRoundManagerBridge.Instance.EliminationsToWin;
                eliminationsText.text = $"Eliminations: {stats?.kills}/{target}";
            }
        }
        #endregion

        #region Event Handlers
        private async void OnMatchEnd(PlayerID? winner)
        {
            if (matchEndPanel != null)
            {
                matchEndPanel.SetActive(true);
            }

            // Unlock cursor so player can click buttons
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Disable player controls
            if (playerObject != null)
            {
                var playerController = playerObject.GetComponent<PlayerController.PlayerController>();
                if (playerController != null)
                {
                    playerController.enabled = false;
                }

                var projectileShooter = playerObject.GetComponent<Resonance.Combat.PlayerShooter>();
                if (projectileShooter != null)
                {
                    projectileShooter.enabled = false;
                }
            }

            // Pause time (optional - uncomment if you want to freeze everything)
            // Time.timeScale = 0f;

            if (winnerText != null)
            {
                string winnerName = $"{winner} Wins!";
                winnerText.text = winnerName;
            }

            // Optionally show basic stats in winner text
            if (winner is PlayerID id && MatchStatBridge.Instance != null)
            {
                PlayerMatchStats? stats = await MatchStatBridge.Instance.GetStats(id);
                if (stats != null && finalStatsText != null)
                {
                    finalStatsText.text = $"Final Score: {stats?.kills} Kills";
                }
            }

            if (waitingForHostText != null)
            {
                if (isServer)
                {
                    waitingForHostText.text = "";
                }
                else
                {
                    waitingForHostText.text = "Waiting for host...";
                }
            }

            if (returnToLobbyButton != null)
            {
                if (!isServer)
                {
                    returnToLobbyButton.gameObject.SetActive(false);
                }
            }

            if (shopGameObject != null)
            {
                shopGameObject.SetActive(false);
            }
        }


        private void OnPlayerKill(GameObject killer, GameObject victim)
        {
            // Optional: Add kill feed notifications here
        }
        #endregion

        #region Public Methods
        public void SetPlayerObject(GameObject player)
        {
            playerObject = player;
            UpdateHUD();
        }

        public void ShowMatchEndScreen()
        {
            if (matchEndPanel != null)
            {
                matchEndPanel.SetActive(true);
            }
        }

        public void HideMatchEndScreen()
        {
            if (matchEndPanel != null)
            {
                matchEndPanel.SetActive(false);
            }
        }
        #endregion
    }
}
