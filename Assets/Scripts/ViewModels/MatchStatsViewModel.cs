using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Resonance.Match;
using Resonance.Helper;
using Resonance.Assemblies.MatchStat;

public class MatchStatsViewModel : MonoBehaviour
{
    public static MatchStatsViewModel Instance { get; private set; }

    public ObservableValue<List<PlayerRanking>> Rankings =
        new(new List<PlayerRanking>());

    private Coroutine _refreshCoroutine;
    private bool _isRefreshing;

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

    public void StartRefreshing()
    {
        if (_isRefreshing) return;

        if (ArenaRoundManagerBridge.GetTemporaryReference() == null)
            return;

        _isRefreshing = true;
        _refreshCoroutine = StartCoroutine(RefreshLoop());
    }

    public void StopRefreshing()
    {
        _isRefreshing = false;

        if (_refreshCoroutine != null)
        {
            StopCoroutine(_refreshCoroutine);
            _refreshCoroutine = null;
        }
    }

    private IEnumerator RefreshLoop()
    {
        while (_isRefreshing)
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
