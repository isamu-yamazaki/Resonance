using System;
using System.Collections.Generic;

namespace Resonance.Assemblies.MatchStat
{
    /// <summary>
    /// Tracks recent damage by all players to calculate who should receive assists.
    /// </summary>
    public class AssistCalculator
    {
        private float assistTimeWindowMs;
        private float assistDamageThreshold;
        private Dictionary<ulong, List<DamageContribution>> recentDamage = new();

        public AssistCalculator(float assistTimeWindowMs, float assistDamageThreshold)
        {
            this.assistTimeWindowMs = assistTimeWindowMs;
            this.assistDamageThreshold = assistDamageThreshold;
        }

        public void RecordDamage(ulong attackerId, ulong victimId, float damageAmount)
        {
            if (attackerId == 0 || victimId == 0 || attackerId == victimId) return;

            if (!recentDamage.ContainsKey(victimId))
            {
                recentDamage[victimId] = new List<DamageContribution>();
            }

            recentDamage[victimId].Add(new DamageContribution
            {
                attackerId = attackerId,
                damageAmount = damageAmount,
                timestampUnixTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            });

            CleanupOldDamage(victimId);
        }

        private void CleanupOldDamage(ulong victimId)
        {
            if (!recentDamage.ContainsKey(victimId)) return;

            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            recentDamage[victimId].RemoveAll(d => currentTime - d.timestampUnixTimeMs > assistTimeWindowMs);
        }

        public HashSet<ulong> GetAssistAttackersForVictim(ulong victimId, ulong killerId)
        {
            var assisters = new HashSet<ulong>();

            if (!recentDamage.ContainsKey(victimId)) return assisters;

            CleanupOldDamage(victimId);

            // Aggregate total damage per attacker
            var damageByAttacker = new Dictionary<ulong, float>();

            foreach (var contribution in recentDamage[victimId])
            {
                // Skip if the contributor is the killer
                if (contribution.attackerId == killerId) continue;

                if (!damageByAttacker.ContainsKey(contribution.attackerId))
                {
                    damageByAttacker[contribution.attackerId] = 0;
                }
                damageByAttacker[contribution.attackerId] += contribution.damageAmount;
            }

            // Check aggregated totals against threshold
            foreach (var kvp in damageByAttacker)
            {
                if (kvp.Value >= assistDamageThreshold)
                {
                    assisters.Add(kvp.Key);
                }
            }

            return assisters;
        }

        public void ClearDamageForVictim(ulong victimId)
        {
            if (recentDamage.ContainsKey(victimId))
            {
                recentDamage[victimId].Clear();
            }
        }

        public void Clear()
        {
            recentDamage.Clear();
        }
    }
}
