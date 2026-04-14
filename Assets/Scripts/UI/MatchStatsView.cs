using UnityEngine;
using System.Collections.Generic;
using Resonance.Assemblies.MatchStat;

public class MatchStatsView : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private LeaderboardRow rowPrefab;

    private readonly List<LeaderboardRow> _spawnedRows = new();
    private MatchStatsViewModel _vm;

    private void Start()
    {
        _vm = MatchStatsViewModel.Instance;

        if (_vm == null)
            _vm = FindObjectOfType<MatchStatsViewModel>();

        if (_vm == null)
        {
            Debug.LogError("MatchStatsViewModel not found in scene");
            return;
        }

        _vm.IsVisible.ChangeEvent += OnVisibilityChanged;
        _vm.Rankings.ChangeEvent += OnRankingsChanged;

        OnVisibilityChanged(_vm.IsVisible.Value);
    }

    private void OnDestroy()
    {
        if (_vm == null) return;

        _vm.IsVisible.ChangeEvent -= OnVisibilityChanged;
        _vm.Rankings.ChangeEvent -= OnRankingsChanged;
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