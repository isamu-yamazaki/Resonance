using PurrNet;
using Resonance.Assemblies.Arena;
using Resonance.Assemblies.MatchStat;

namespace Resonance.Match
{
    [System.Serializable]
    public class NetworkedPlayerRanking
    {
        public PlayerID player;
        public PlayerMatchStats stats;

        public NetworkedPlayerRanking(PlayerRanking ranking)
        {
            player = OwnerIDExtractor.UlongToPlayerId(ranking.player);
            stats = ranking.stats;
        }
    }
}
