using System.Collections.Generic;
using Resonance.Assemblies.MatchStat;

namespace Resonance.Assemblies.Arena.PointGains
{
    public class PlayerDamagePointGain : BasePointGain
    {
        public PlayerDamagePointGain(MatchStatTracker matchStatTracker, ArenaRoundManager arenaRoundManager, ArenaRatingManager arenaRatingManager)
            : base(matchStatTracker, arenaRoundManager, arenaRatingManager, new List<string> { "consecHit" })
        {
        }

        public override void OnDamageDealt(ulong attackerId, ulong victimId, float amount)
        {
            float points = amount;

            foreach (var key in multiplierKeys)
            {
                points *= arenaRatingManager.GetMultiplierValue(key, attackerId);
            }

            matchStatTracker.RecordRating(attackerId, points);
        }
    }
}