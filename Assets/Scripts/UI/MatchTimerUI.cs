using Resonance.Match;
using TMPro;
using UnityEngine;

public class MatchTimerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private MatchTimerViewModel viewModel;

    private void Start()
    {
        viewModel.FormattedTime.ChangeEvent += UpdateText;

        if (MatchLogicNetworkAdapter.Instance != null)
        {
            MatchLogicNetworkAdapter.Instance
                .ArenaRoundManager
                .OnMatchTimerElapsed += viewModel.SetTime;
        }
    }

    private void OnDestroy()
    {
        viewModel.FormattedTime.ChangeEvent -= UpdateText;

        if (MatchLogicNetworkAdapter.Instance != null)
        {
            MatchLogicNetworkAdapter.Instance
                .ArenaRoundManager
                .OnMatchTimerElapsed -= viewModel.SetTime;
        }
    }

    private void UpdateText(string time)
    {
        timerText.text = time;
    }
}