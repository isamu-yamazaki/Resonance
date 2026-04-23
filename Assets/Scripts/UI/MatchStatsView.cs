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
        // Spawn any missing rows
        while (_spawnedRows.Count < rankings.Count)
        {
            var row = Instantiate(rowPrefab, contentRoot);
            _spawnedRows.Add(row);
        }

        // Hide any extra rows
        for (int i = 0; i < _spawnedRows.Count; i++)
        {
            if (i < rankings.Count)
            {
                _spawnedRows[i].gameObject.SetActive(true);
                _spawnedRows[i].Setup(i + 1, rankings[i]);
            }
            else
            {
                _spawnedRows[i].gameObject.SetActive(false);
            }
        }
    }
}
