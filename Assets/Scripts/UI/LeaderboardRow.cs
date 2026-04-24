using Resonance.Assemblies.LobbySystem;
using Resonance.Assemblies.MatchStat;
using Resonance.Match;
using TMPro;
using UnityEngine;

public class LeaderboardRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI killsText;
    [SerializeField] private TextMeshProUGUI deathsText;
    [SerializeField] private TextMeshProUGUI assistsText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI ratingText;

    public void Setup(int rank, PlayerRanking ranking)
    {
        rankText.text = rank.ToString();

        var playerId = OwnerIDExtractor.UlongToPlayerId(ranking.player);
        var displayName = PlayerIdToLobbyMemberIdMap.Instance?.GetDisplayName(playerId);
        nameText.text = displayName ?? ranking.player.ToString();

        killsText.text = ranking.stats.kills.ToString();
        deathsText.text = ranking.stats.deaths.ToString();
        assistsText.text = ranking.stats.assists.ToString();
        damageText.text = ranking.stats.totalDamageDealt.ToString();
        ratingText.text = ranking.stats.rating.ToString();
    }
}
