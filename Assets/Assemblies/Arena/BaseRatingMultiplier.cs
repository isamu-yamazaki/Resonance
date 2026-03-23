using Resonance.Assemblies.MatchStat;

namespace Resonance.Assemblies.Arena
{
    public abstract class BaseRatingMultiplier
    {
        protected MatchStatTracker matchStatTracker;
        protected ArenaRoundManager arenaRoundManager;

        protected BaseRatingMultiplier(MatchStatTracker matchStatTracker, ArenaRoundManager arenaRoundManager)
        {
            this.matchStatTracker = matchStatTracker;
            this.arenaRoundManager = arenaRoundManager;
        }

        public abstract float GetValue(ulong playerId);
        public abstract string Key { get; }

        public virtual void OnKill(ulong killerId, ulong victimId) { }
        public virtual void OnDeath(ulong playerId) { }
        public virtual void OnDamageDealt(ulong attackerId, ulong victimId, float amount) { }
        public virtual void OnMiss(ulong playerId) { }
    }
}