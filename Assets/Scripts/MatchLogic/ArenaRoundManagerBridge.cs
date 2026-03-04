namespace Resonance.Match
{
    /// <summary>
    /// Facade for accessing ArenaRoundManagerNetworkAdapter via singleton.
    /// </summary>
    public static class ArenaRoundManagerBridge
    {
        public static ArenaRoundManagerNetworkAdapter Instance =>
            MatchLogicNetworkAdapter.Instance?.ArenaRoundManager;
    }
}
