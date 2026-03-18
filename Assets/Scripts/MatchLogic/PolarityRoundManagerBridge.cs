namespace Resonance.Match
{
    /// <summary>
    /// Facade for accessing PolarityRoundManagerNetworkAdapter via singleton.
    /// Returns null if the active round manager is not a PolarityRoundManagerNetworkAdapter.
    /// </summary>
    public static class PolarityRoundManagerBridge
    {
        public static PolarityRoundManagerNetworkAdapter Instance =>
            MatchLogicNetworkAdapter.Instance?.ActiveRoundManager as PolarityRoundManagerNetworkAdapter;
    }
}
