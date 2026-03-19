using System.Collections.Generic;
using Resonance.Assemblies.MatchStat;

namespace Resonance.Assemblies.Arena
{
    public abstract class BasePointGain
    {
        protected MatchStatTracker matchStatTracker;
        protected ArenaRoundManager arenaRoundManager;
        protected ArenaRatingManager arenaRatingManager;
        protected List<string> multiplierKeys;
        

        protected BasePointGain(MatchStatTracker matchStatTracker, ArenaRoundManager arenaRoundManager, ArenaRatingManager arenaRatingManager, List<string> multiplierKeys)
        {
            this.matchStatTracker = matchStatTracker;
            this.arenaRoundManager = arenaRoundManager;
            this.arenaRatingManager = arenaRatingManager;
            this.multiplierKeys = multiplierKeys;
        }

        public virtual void OnKill(ulong killerId, ulong victimId) { }
        public virtual void OnDeath(ulong playerId) { }
        public virtual void OnDamageDealt(ulong attackerId, ulong victimId, float amount) { }
        public virtual void OnMiss(ulong playerId) { }
    }
}