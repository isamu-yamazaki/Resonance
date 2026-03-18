using Resonance.Match;
using UnityEngine;

public class SampleMatchTimerListener : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (ArenaRoundManagerBridge.Instance != null)
        {
            Debug.Log("[MatchTimerListener] Subscribing to match timer events");
            ArenaRoundManagerBridge.Instance.OnMatchTimerElapsed += OnMatchTimerElapsed;
        }
    }

    private void OnMatchTimerElapsed(double secondsRemaining)
    {
        Debug.Log($"[MatchTimerListener] Seconds remaining: {secondsRemaining}");
    }
}
