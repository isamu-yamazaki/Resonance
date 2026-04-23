using UnityEngine;
using PurrNet;
using Resonance.Match;

namespace Resonance.UI
{
    // Listens for the match-end event and shows the MatchEndOverlayView. Attach alongside
    // InGameViewRouterBridge. Arena-only for now; Polarity's OnMatchEnd has a different
    // payload (TeamId) and is not wired here.
    public class MatchEndOverlayListener : MonoBehaviour
    {
        private void Start()
        {
            if (MatchLogicNetworkAdapter.Instance != null)
            {
                MatchLogicNetworkAdapter.Instance.OnFinishedConfiguring += HandleFinishedConfiguring;

                if (MatchLogicNetworkAdapter.Instance.HasFinishedConfiguring)
                {
                    HandleFinishedConfiguring();
                }
            }
        }

        private void OnDestroy()
        {
            if (MatchLogicNetworkAdapter.Instance != null)
            {
                MatchLogicNetworkAdapter.Instance.OnFinishedConfiguring -= HandleFinishedConfiguring;
            }

            var arenaRoundManager = ArenaRoundManagerBridge.GetTemporaryReference();
            if (arenaRoundManager != null)
            {
                arenaRoundManager.OnMatchEnd -= HandleMatchEnd;
            }
        }

        private void HandleFinishedConfiguring()
        {
            var arenaRoundManager = ArenaRoundManagerBridge.GetTemporaryReference();
            if (arenaRoundManager != null)
            {
                arenaRoundManager.OnMatchEnd += HandleMatchEnd;
            }
        }

        private void HandleMatchEnd(PlayerID? winner)
        {
            if (MatchEndOverlayView.Instance != null)
            {
                MatchEndOverlayView.Instance.PresentWinner(winner);
            }

            if (InGameViewRouterBridge.Instance != null)
            {
                InGameViewRouterBridge.Instance.ShowMatchEnd();
            }
        }
    }
}
