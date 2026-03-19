using System.Collections.Generic;
using Resonance.Assemblies.MatchStat;

namespace Resonance.Assemblies.Arena.PointGains
{
    public class KillPointGain : BasePointGain
    {
        private const float BasePoints = 100f;

        public KillPointGain(MatchStatTracker matchStatTracker, ArenaRoundManager arenaRoundManager, ArenaRatingManager arenaRatingManager)
            : base(matchStatTracker, arenaRoundManager, arenaRatingManager, new List<string> { "killstreak" })
        {
        }

        public override void OnKill(ulong killerId, ulong victimId)
        {
            float points = BasePoints;

            foreach (var key in multiplierKeys)
            {
                points *= arenaRatingManager.GetMultiplierValue(key, killerId);
            }

            matchStatTracker.RecordRating(killerId, points);
        }
    }
}