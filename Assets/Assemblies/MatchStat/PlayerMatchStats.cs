using PurrNet.Packing;

namespace Resonance.Assemblies.MatchStat
{
    [System.Serializable]
    public struct PlayerMatchStats: IPackedAuto
    {
        public int kills;
        public int deaths;
        public int assists;
        public int killStreak;
        public int bestKillStreak;
        
        public float rating;

        /// <summary>
        /// Total damage dealt by the player to all players for the game.
        /// </summary>
        public float totalDamageDealt;

        public float KDA => deaths == 0 ? (kills + assists) : (float)(kills + assists) / deaths;

        public override string ToString()
        {
            return $"K/D/A: {kills}/{deaths}/{assists} | KDA: {KDA:F2} | Streak: {killStreak}";
        }

        private PlayerMatchStats With(
            int? kills = null,
            int? deaths = null,
            int? assists = null,
            int? killStreak = null,
            int? bestKillStreak = null,
            float? totalDamageDealt = null, 
            float? rating = null)
        {
            return new PlayerMatchStats
            {
                kills = kills ?? this.kills,
                deaths = deaths ?? this.deaths,
                assists = assists ?? this.assists,
                killStreak = killStreak ?? this.killStreak,
                bestKillStreak = bestKillStreak ?? this.bestKillStreak,
                totalDamageDealt = totalDamageDealt ?? this.totalDamageDealt,
                rating = rating ?? this.rating
            };
        }

        public PlayerMatchStats RecordKill()
        {
            int newKillStreak = killStreak + 1;
            return With(
                kills: kills + 1,
                killStreak: newKillStreak,
                bestKillStreak: newKillStreak > bestKillStreak ? newKillStreak : bestKillStreak
            );
        }

        public PlayerMatchStats RecordDeath()
        {
            return With(deaths: deaths + 1, killStreak: 0);
        }

        public PlayerMatchStats RecordAssist()
        {
            return With(assists: assists + 1);
        }

        public PlayerMatchStats RecordDamage(float damage)
        {
            return With(totalDamageDealt: totalDamageDealt + damage);
        }

        public PlayerMatchStats RecordRating(float amount)
        {
            return With(rating: rating + amount);
        }
    }
}
