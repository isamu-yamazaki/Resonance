using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using PurrNet;
using Resonance.Assemblies.LobbySystem;
using Resonance.Assemblies.MatchStat;
using Resonance.Assemblies.UISystem;
using Resonance.GameBootstrap;
using Resonance.Match;

public class MatchStatsView : MonoBehaviour, IOverlayView
{
    public static string Key => nameof(MatchStatsView);
    string IOverlayView.Key => Key;

    [SerializeField] private GameObject root;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private LeaderboardRow rowPrefab;

    private readonly List<LeaderboardRow> _spawnedRows = new();
    private MatchStatsModel _model;
    private PlayerIdToMatchMemberIdMap _playerIdMap;
    private NetworkedMatchDataHolder _matchDataHolder;

    private readonly Dictionary<PlayerID, string> _cachedPlayerIDToDisplayNameMap = new();

    private void Start()
    {
        _model = MatchStatsModel.Instance;

        if (_model == null)
            _model = FindFirstObjectByType<MatchStatsModel>();

        if (_model == null)
        {
            Debug.LogError("MatchStatsModel not found in scene");
            return;
        }

        root.SetActive(false);
        _model.Rankings.ChangeEvent += OnRankingsChanged;

        _playerIdMap = PlayerIdToMatchMemberIdMap.Instance;
        if (_playerIdMap != null)
        {
            _playerIdMap.OnDictionaryChanged += OnLobbyMapChanged;
        }

        _matchDataHolder = FindFirstObjectByType<NetworkedMatchDataHolder>();

        _ = UpdateDisplayNameCacheAndRenderRows();
    }

    private void OnDestroy()
    {
        if (_model != null)
        {
            _model.Rankings.ChangeEvent -= OnRankingsChanged;
        }

        if (_playerIdMap != null)
        {
            _playerIdMap.OnDictionaryChanged -= OnLobbyMapChanged;
        }
    }

    public void OnShow(OverlayViewActions viewActions)
    {
        root.SetActive(true);
    }

    public void OnHide()
    {
        root.SetActive(false);
    }

    private void OnRankingsChanged(List<PlayerRanking> rankings)
    {
        RenderRows(rankings);
    }

    private void OnLobbyMapChanged()
    {
        if (_model == null) return;
        _ = UpdateDisplayNameCacheAndRenderRows();
    }

    private async Task UpdateDisplayNameCacheAndRenderRows()
    {
        await UpdateDisplayNameCache();
        RenderRows(_model.Rankings.Value);
    }

    private async Task UpdateDisplayNameCache()
    {
        var map = _playerIdMap?.GetPlayerIdentityMap();
        if (map == null || _matchDataHolder == null) return;
        Debug.Log($"[{nameof(MatchStatsView)}] {map.Count} pair(s) in player identity map");
        foreach (var pair in map)
        {
            Debug.Log($"[{nameof(MatchStatsView)}] Getting display name for {pair.Key} {pair.Value}");
            var displayName = await _matchDataHolder.GetDisplayName(pair.Value);
            if (displayName != null)
            {
                _cachedPlayerIDToDisplayNameMap.Add(pair.Key, displayName);
            }
        }
    }

    private void RenderRows(List<PlayerRanking> rankings)
    {
        while (_spawnedRows.Count < rankings.Count)
        {
            var row = Instantiate(rowPrefab, contentRoot);
            _spawnedRows.Add(row);
        }

        for (int i = 0; i < _spawnedRows.Count; i++)
        {
            if (i < rankings.Count)
            {
                var ranking = rankings[i];
                var playerId = OwnerIDExtractor.UlongToPlayerId(ranking.player);
                var displayName = _cachedPlayerIDToDisplayNameMap.GetValueOrDefault(playerId) ?? playerId.ToString();

                _spawnedRows[i].gameObject.SetActive(true);
                _spawnedRows[i].Setup(i + 1, ranking, displayName);
            }
            else
            {
                _spawnedRows[i].gameObject.SetActive(false);
            }
        }
    }
}