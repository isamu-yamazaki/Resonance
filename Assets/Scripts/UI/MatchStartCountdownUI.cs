using TMPro;
using UnityEngine;
using System.Collections;
using Resonance.Match;

public class MatchStartCountdownUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI countdownText;

    private void OnEnable()
    {
        if (ArenaRoundManagerBridge.Instance != null)
        {
            ArenaRoundManagerBridge.Instance.OnMatchCountdownStart += HandleCountdownStart;
        }
    }

    private void OnDisable()
    {
        if (ArenaRoundManagerBridge.Instance != null)
        {
            ArenaRoundManagerBridge.Instance.OnMatchCountdownStart -= HandleCountdownStart;
        }
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