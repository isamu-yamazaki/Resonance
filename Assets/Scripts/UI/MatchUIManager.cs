using UnityEngine;
using TMPro;
using Resonance.Match;
using Resonance.Assemblies.MatchStat;
using PurrNet;

namespace Resonance.UI
{
    public class MatchUIManager : NetworkBehaviour
    {
        [Header("HUD Elements")]
        [SerializeField] private TextMeshProUGUI kdaText;
        [SerializeField] private TextMeshProUGUI killStreakText;
        [SerializeField] private TextMeshProUGUI eliminationsText;

        [Header("Settings")]
        [SerializeField] private GameObject playerObject; // Assign the player to track
        [SerializeField] private bool showKillStreak = true;

        private void Start()
        {
            if (killStreakText != null && showKillStreak)
            {
                killStreakText.text = "Kill Streak: 0";
                killStreakText.gameObject.SetActive(true);
            }

            UpdateHUD();
        }

        private void Update()
        {
            UpdateHUD();
        }

        private async void UpdateHUD()
        {
            var matchStats = MatchStatBridge.GetTemporaryReference();
            if (playerObject == null || matchStats == null) return;

            PlayerMatchStats? stats = await matchStats.GetStats(playerObject);
            if (stats == null) return;

            if (kdaText != null)
            {
                kdaText.text = $"K/D/A: {stats?.kills}/{stats?.deaths}/{stats?.assists} | KDA: {stats?.KDA:F2}";
            }

            if (killStreakText != null && showKillStreak)
            {
                killStreakText.text = $"Kill Streak: {stats?.killStreak}";
                killStreakText.gameObject.SetActive(true);
            }

            var arenaRoundManager = ArenaRoundManagerBridge.GetTemporaryReference();
            if (eliminationsText != null && arenaRoundManager != null)
            {
                float target = arenaRoundManager.RatingToWin;
                eliminationsText.text = $"Rating: {stats?.rating:F0}/{target}";
            }
        }

        public void SetPlayerObject(GameObject player)
        {
            playerObject = player;
            UpdateHUD();
        }
    }
}
