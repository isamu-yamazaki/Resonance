using PurrNet.Packing;

namespace Resonance.Assemblies.MatchStat
{
    [System.Serializable]
    public struct PlayerRanking : IPackedAuto
    {
        public ulong player;
        public PlayerMatchStats stats;
    }
}
