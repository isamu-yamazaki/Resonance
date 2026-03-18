using Resonance.Assemblies.MatchStat;
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

    public void Setup(int rank, PlayerRanking ranking)
    {
        rankText.text = rank.ToString();
        nameText.text = $"{ranking.player}";

        killsText.text = ranking.stats.kills.ToString();
        deathsText.text = ranking.stats.deaths.ToString();
        assistsText.text = ranking.stats.assists.ToString();
        damageText.text = ranking.stats.totalDamageDealt.ToString();
    }
}
