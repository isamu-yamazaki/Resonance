using UnityEngine;
using System.Collections.Generic;
using Resonance.Assemblies.MatchStat;
using Resonance.Assemblies.UISystem;

public class MatchStatsView : MonoBehaviour, IOverlayView
{
    public static string Key => nameof(MatchStatsView);
    string IOverlayView.Key => Key;

    [SerializeField] private GameObject root;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private LeaderboardRow rowPrefab;

    private readonly List<LeaderboardRow> _spawnedRows = new();
    private MatchStatsViewModel _vm;

    private void Start()
    {
        _vm = MatchStatsViewModel.Instance;

        if (_vm == null)
            _vm = FindFirstObjectByType<MatchStatsViewModel>();

        if (_vm == null)
        {
            Debug.LogError("MatchStatsViewModel not found in scene");
            return;
        }

        root.SetActive(false);
        _vm.Rankings.ChangeEvent += OnRankingsChanged;
    }

    private void OnDestroy()
    {
        if (_vm == null) return;

        _vm.Rankings.ChangeEvent -= OnRankingsChanged;
    }

    public void OnShow(OverlayViewActions viewActions)
    {
        root.SetActive(true);
        _vm?.StartRefreshing();
    }

    public void OnHide()
    {
        root.SetActive(false);
        _vm?.StopRefreshing();
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
