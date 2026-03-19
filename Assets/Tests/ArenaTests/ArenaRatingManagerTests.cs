using NUnit.Framework;
using Resonance.Assemblies.MatchStat;
using Resonance.Assemblies.Arena;

public class ArenaRatingManagerTests
{
    private MatchStatTracker statTracker;
    private ArenaRoundManager roundManager;
    private ArenaRatingManager ratingManager;

    private ulong killerId = 1;
    private ulong victimId = 2;

    [SetUp]
    public void Setup()
    {
        statTracker = new MatchStatTracker();
        roundManager = new ArenaRoundManager(statTracker);
        ratingManager = new ArenaRatingManager(statTracker, roundManager);

        statTracker.RegisterPlayer(killerId);
        statTracker.RegisterPlayer(victimId);
    }

    [Test]
    public void OneKill_Awards100Points()
    {
        statTracker.RecordKill(killerId, victimId);

        var stats = statTracker.GetStats(killerId);
        Assert.AreEqual(100f, stats.rating);
    }

    [Test]
    public void ThreeKills_DeathBetweenEach_Awards300Points()
    {
        statTracker.RecordKill(killerId, victimId);
        statTracker.RecordDeath(killerId);

        statTracker.RecordKill(killerId, victimId);
        statTracker.RecordDeath(killerId);

        statTracker.RecordKill(killerId, victimId);

        var stats = statTracker.GetStats(killerId);
        Assert.AreEqual(300f, stats.rating);
    }

    [Test]
    public void TwoKillsThenDeathThenOneKill_Awards400Points()
    {
        statTracker.RecordKill(killerId, victimId);
        statTracker.RecordKill(killerId, victimId);
        statTracker.RecordDeath(killerId);

        statTracker.RecordKill(killerId, victimId);

        var stats = statTracker.GetStats(killerId);
        Assert.AreEqual(400f, stats.rating);
    }

    [Test]
    public void ThreeKills_NoDeaths_Awards600Points()
    {
        statTracker.RecordKill(killerId, victimId);
        statTracker.RecordKill(killerId, victimId);
        statTracker.RecordKill(killerId, victimId);

        var stats = statTracker.GetStats(killerId);
        Assert.AreEqual(600f, stats.rating);
    }
}