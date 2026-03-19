using Resonance.Assemblies.MatchStat;

namespace Resonance.Assemblies.Arena.Multipliers
{
    public class KillstreakMultiplier : BaseRatingMultiplier
    {
        public override string Key => "killstreak";

        public KillstreakMultiplier(MatchStatTracker matchStatTracker, ArenaRoundManager arenaRoundManager)
            : base(matchStatTracker, arenaRoundManager)
        {
        }

        public override float GetValue(ulong playerId)
        {
            return matchStatTracker.GetStats(playerId).killStreak;
        }
    }
}