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
        if (ArenaRoundManagerBridge.Instance != null)
        {
            Debug.Log("[MatchTimerListener] Subscribing to match timer events");
            ArenaRoundManagerBridge.Instance.OnMatchTimerElapsed += OnMatchTimerElapsed;
        }
    }

    private void OnDestroy()
    {
        if (MatchLogicNetworkAdapter.Instance != null)
            MatchLogicNetworkAdapter.Instance.OnFinishedConfiguring -= OnMatchLogicConfigured;

        if (ArenaRoundManagerBridge.Instance != null)
            ArenaRoundManagerBridge.Instance.OnMatchTimerElapsed -= OnMatchTimerElapsed;
    }

    private void OnMatchTimerElapsed(double secondsRemaining)
    {
        Debug.Log($"[MatchTimerListener] Seconds remaining: {secondsRemaining}");
    }
}
