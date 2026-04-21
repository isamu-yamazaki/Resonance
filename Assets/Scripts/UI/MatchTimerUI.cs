using Resonance.Match;
using TMPro;
using UnityEngine;

public class MatchTimerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private MatchTimerViewModel viewModel;

    private void Start()
    {
        if (viewModel == null)
            viewModel = FindObjectOfType<MatchTimerViewModel>();
        
        viewModel.FormattedTime.ChangeEvent += UpdateText;

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
            arenaRoundManager.OnMatchTimerElapsed += viewModel.SetTime;
    }

    private void OnDestroy()
    {
        viewModel.FormattedTime.ChangeEvent -= UpdateText;

        if (MatchLogicNetworkAdapter.Instance != null)
            MatchLogicNetworkAdapter.Instance.OnFinishedConfiguring -= OnMatchLogicConfigured;

        var arenaRoundManager = ArenaRoundManagerBridge.GetTemporaryReference();
        if (arenaRoundManager != null)
            arenaRoundManager.OnMatchTimerElapsed -= viewModel.SetTime;
    }

    private void UpdateText(string time)
    {
        timerText.text = time;
    }
}
