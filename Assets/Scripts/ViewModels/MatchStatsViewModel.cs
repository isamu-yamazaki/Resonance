using System.Collections.Generic;
using UnityEngine;
using Resonance.Match;
using Resonance.Helper;
using Resonance.Assemblies.MatchStat;

public class MatchStatsViewModel : MonoBehaviour
{
    public static MatchStatsViewModel Instance { get; private set; }

    public ObservableValue<bool> IsVisible = new(false);
    public ObservableValue<List<PlayerRanking>> Rankings =
        new(new List<PlayerRanking>());

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async void Show()
    {
        IsVisible.Value = true;

        if (ArenaRoundManagerBridge.Instance == null)
            return;

        var leaderboard = await ArenaRoundManagerBridge.Instance.GetLeaderboard();
        Rankings.Value = leaderboard;
    }

    public void Hide()
    {
        IsVisible.Value = false;
    }

    public void Toggle()
    {
        IsVisible.Value = !IsVisible.Value;
    }
}
