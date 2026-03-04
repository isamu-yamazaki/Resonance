using NUnit.Framework;
using Resonance.Assemblies.MatchStat;
public class PlayerMatchStatsTests
{
    [Test]
    public void RecordKill_ShouldIncrementKillAndStreak()
    {
        PlayerMatchStats stats = new() { kills = 2, killStreak = 1, bestKillStreak = 1 };
        PlayerMatchStats newStats = stats.RecordKill();

        Assert.AreEqual(3, newStats.kills);
        Assert.AreEqual(2, newStats.killStreak);
        Assert.AreEqual(2, newStats.bestKillStreak);
    }

    [Test]
    public void RecordDeath_ShouldIncrementDeathAndResetStreak()
    {
        PlayerMatchStats stats = new() { deaths = 1, killStreak = 3, bestKillStreak = 5 };
        PlayerMatchStats newStats = stats.RecordDeath();

        Assert.AreEqual(2, newStats.deaths);
        Assert.AreEqual(0, newStats.killStreak);
        Assert.AreEqual(5, newStats.bestKillStreak); // Best streak should remain unchanged
    }

    [Test]
    public void RecordAssist_ShouldIncrementAssist()
    {
        PlayerMatchStats stats = new() { assists = 2 };
        PlayerMatchStats newStats = stats.RecordAssist();

        Assert.AreEqual(3, newStats.assists);
    }

    [Test]
    public void RecordDamage_ShouldIncrementDamage()
    {
        PlayerMatchStats stats = new() { totalDamageDealt = 20 };
        PlayerMatchStats newStats = stats.RecordDamage(50);

        Assert.AreEqual(70, newStats.totalDamageDealt);
    }

    [Test]
    public void KDA_ShouldCalculateCorrectlyWithNoDeaths()
    {
        PlayerMatchStats stats1 = new() { kills = 5, assists = 3, deaths = 0 };
        Assert.AreEqual(8, stats1.KDA);
    }

    [Test]
    public void KDA_ShouldCalculateCorrectlyWithDeaths()
    {
        PlayerMatchStats stats2 = new() { kills = 5, assists = 3, deaths = 4 };
        Assert.AreEqual(2, stats2.KDA);
    }

    [Test]
    public void ToString_ShouldFormatCorrectly()
    {
        PlayerMatchStats stats = new() { kills = 10, deaths = 2, assists = 5, killStreak = 3 };
        string result = stats.ToString();

        Assert.AreEqual("K/D/A: 10/2/5 | KDA: 7.50 | Streak: 3", result);
    }
}
