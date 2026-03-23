using Resonance.Assemblies.MatchStat;

namespace Resonance.Assemblies.Arena.Multipliers
{
    public class ConsecHitMultiplier : BaseRatingMultiplier
    {
        public override string Key => "consecHit";

        private float currentValue = 1f;
        private const float incrementPerHit = 0.5f;

        public ConsecHitMultiplier(MatchStatTracker matchStatTracker, ArenaRoundManager arenaRoundManager)
            : base(matchStatTracker, arenaRoundManager)
        {
        }

        public override float GetValue(ulong playerId) => currentValue;

        public override void OnDamageDealt(ulong attackerId, ulong victimId, float amount)
        {
            currentValue += incrementPerHit;
        }

        public override void OnMiss(ulong playerId)
        {
            currentValue = 1f;
        }
    }
}