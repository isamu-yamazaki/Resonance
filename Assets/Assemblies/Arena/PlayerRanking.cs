using Resonance.Assemblies.MatchStat;

namespace Resonance.Assemblies.Arena
{
    [System.Serializable]
    public class PlayerRanking
    {
        public ulong player;
        public PlayerMatchStats stats;

        // Ranking is based on item index, not stored property
    }
}
