using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Resonance.Match;
using Resonance.Helper;
using Resonance.Assemblies.MatchStat;

public class MatchStatsModel : MonoBehaviour
{
    public static MatchStatsModel Instance { get; private set; }

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

    private void Start()
    {
        StartCoroutine(RefreshLoop());
    }

    private IEnumerator RefreshLoop()
    {
        var wait = new WaitForSeconds(0.5f);

        while (true)
        {
            var arenaRoundManager = ArenaRoundManagerBridge.GetTemporaryReference();
            if (arenaRoundManager == null)
            {
                yield return wait;
                continue;
            }

            var fetch = arenaRoundManager.GetLeaderboard();
            yield return new WaitUntil(() => fetch.IsCompleted);
            Rankings.Value = fetch.Result;

            yield return wait;
        }
    }
}
