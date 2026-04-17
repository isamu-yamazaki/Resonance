using System.Collections;
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

    private Coroutine _refreshCoroutine;

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

    public void Show()
    {
        IsVisible.Value = true;

        if (ArenaRoundManagerBridge.GetTemporaryReference() == null)
            return;

        if (_refreshCoroutine != null)
            StopCoroutine(_refreshCoroutine);

        _refreshCoroutine = StartCoroutine(RefreshLoop());
    }

    public void Hide()
    {
        IsVisible.Value = false;

        if (_refreshCoroutine != null)
        {
            StopCoroutine(_refreshCoroutine);
            _refreshCoroutine = null;
        }
    }

    public void Toggle()
    {
        if (IsVisible.Value) Hide();
        else Show();
    }

    private IEnumerator RefreshLoop()
    {
        while (IsVisible.Value)
        {
            var arenaRoundManager = ArenaRoundManagerBridge.GetTemporaryReference();
            if (arenaRoundManager == null)
                yield break;

            var fetch = arenaRoundManager.GetLeaderboard();
            yield return new WaitUntil(() => fetch.IsCompleted);
            Rankings.Value = fetch.Result;

            yield return new WaitForSeconds(0.5f);
        }
    }
}