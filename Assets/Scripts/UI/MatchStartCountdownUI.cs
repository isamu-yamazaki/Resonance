using TMPro;
using UnityEngine;
using System.Collections;
using Resonance.Match;
using System;

public class MatchStartCountdownUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI countdownText;

    private void OnEnable()
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
            arenaRoundManager.OnMatchCountdownStart += HandleCountdownStart;
    }

    private void OnDisable()
    {
        var arenaRoundManager = ArenaRoundManagerBridge.GetTemporaryReference();
        if (arenaRoundManager != null)
            arenaRoundManager.OnMatchCountdownStart -= HandleCountdownStart;

        if (MatchLogicNetworkAdapter.Instance != null)
            MatchLogicNetworkAdapter.Instance.OnFinishedConfiguring -= OnMatchLogicConfigured;
    }

    private void HandleCountdownStart(float seconds)
    {
        StartCoroutine(CountdownRoutine(seconds));
    }

    private IEnumerator CountdownRoutine(float seconds)
    {
        int time = Mathf.CeilToInt(seconds);

        while (time > 0)
        {
            countdownText.text = time.ToString();
            yield return new WaitForSeconds(1f);
            time--;
        }

        countdownText.text = "GO!";
        yield return new WaitForSeconds(0.5f);

        countdownText.text = "";
    }
}
