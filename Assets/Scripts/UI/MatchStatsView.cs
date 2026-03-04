using UnityEngine;
using System.Collections.Generic;
using Resonance.Assemblies.Arena;
using Resonance.Match;

public class MatchStatsView : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private LeaderboardRow rowPrefab;

    private readonly List<LeaderboardRow> _spawnedRows = new();

    private void Awake()
    {
        var vm = MatchStatsViewModel.Instance;

        if (vm == null)
        {
            Debug.LogError("MatchStatsViewModel not found in scene");
            return;
        }

        vm.IsVisible.ChangeEvent += OnVisibilityChanged;
        vm.Rankings.ChangeEvent += OnRankingsChanged;

        // Apply initial visibility state
        OnVisibilityChanged(vm.IsVisible.Value);
    }

    private void OnDestroy()
    {
        if (MatchStatsViewModel.Instance == null)
            return;

        MatchStatsViewModel.Instance.IsVisible.ChangeEvent -= OnVisibilityChanged;
        MatchStatsViewModel.Instance.Rankings.ChangeEvent -= OnRankingsChanged;
    }

    private void OnVisibilityChanged(bool visible)
    {
        root.SetActive(visible);
    }

    private void OnRankingsChanged(List<PlayerRanking> rankings)
    {
        foreach (var row in _spawnedRows)
            Destroy(row.gameObject);

        _spawnedRows.Clear();

        for (int i = 0; i < rankings.Count; i++)
        {
            var row = Instantiate(rowPrefab, contentRoot);
            row.Setup(i + 1, rankings[i]);
            _spawnedRows.Add(row);
        }
    }
}