using Resonance.Match;
using UnityEngine;

public class SampleMatchTimerListener : MonoBehaviour
{
    void Start()
    {
        if (MatchLogicNetworkAdapter.Instance != null)
        {
            MatchLogicNetworkAdapter.Instance.OnFinishedConfiguring += OnMatchLogicConfigured;

            if (MatchLogicNetworkAdapter.Instance.HasFinishedConfiguring)
                OnMatchLogicConfigured();
        }
    }

    private void OnMatchLogicConfigured()
    {
        var arenaRoundManager = ArenaRoundManagerBridge.GetTemporaryReference();
        if (arenaRoundManager != null)
        {
            Debug.Log("[MatchTimerListener] Subscribing to match timer events");
            arenaRoundManager.OnMatchTimerElapsed += OnMatchTimerElapsed;
        }
    }

    private void OnDestroy()
    {
        if (MatchLogicNetworkAdapter.Instance != null)
            MatchLogicNetworkAdapter.Instance.OnFinishedConfiguring -= OnMatchLogicConfigured;

        var arenaRoundManager = ArenaRoundManagerBridge.GetTemporaryReference();
        if (arenaRoundManager != null)
            arenaRoundManager.OnMatchTimerElapsed -= OnMatchTimerElapsed;
    }

    private void OnMatchTimerElapsed(double secondsRemaining)
    {
        Debug.Log($"[MatchTimerListener] Seconds remaining: {secondsRemaining}");
    }
}
