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

    // -------------------------------------------------------------------------
    // Kill tests
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    // Damage tests
    // -------------------------------------------------------------------------

    [Test]
    public void SingleDamageHit_AwardsDamageAsPoints()
    {
        statTracker.RecordDamage(killerId, victimId, 50f);

        var stats = statTracker.GetStats(killerId);
        // First hit: 50 * 1.5 = 75
        Assert.AreEqual(75f, stats.rating);
    }

    [Test]
    public void ConsecutiveHits_IncreaseMultiplier_SecondHitAwardsMore()
    {
        statTracker.RecordDamage(killerId, victimId, 50f);
        statTracker.RecordDamage(killerId, victimId, 50f);

        var stats = statTracker.GetStats(killerId);
        // First hit: 50 * 1.5 = 75
        // Second hit: 50 * 2.0 = 100
        // Total: 175
        Assert.AreEqual(175f, stats.rating);
    }

    [Test]
    public void MissResetsConsecHit_NextHitBackToBase()
    {
        statTracker.RecordDamage(killerId, victimId, 50f);
        statTracker.RecordDamage(killerId, victimId, 50f);
        statTracker.RecordMiss(killerId);
        statTracker.RecordDamage(killerId, victimId, 50f);

        var stats = statTracker.GetStats(killerId);
        // First hit: 50 * 1.5 = 75
        // Second hit: 50 * 2.0 = 100
        // Miss resets
        // Third hit: 50 * 1.5 = 75
        // Total: 250
        Assert.AreEqual(250f, stats.rating);
    }

    // -------------------------------------------------------------------------
    // Combined tests
    // -------------------------------------------------------------------------

    [Test]
    public void KillAndDamage_AccumulatesRatingAcrossBothPointGains()
    {
        statTracker.RecordDamage(killerId, victimId, 50f);
        statTracker.RecordKill(killerId, victimId);

        var stats = statTracker.GetStats(killerId);
        // Damage: 50 * 1.5 = 75
        // Kill: 100 * 1 = 100
        // Total: 175
        Assert.AreEqual(175f, stats.rating);
    }

    [Test]
    public void SloppyPlayer_VsCleanPlayer_HasVastlyLessRating()
    {
        ulong cleanPlayer = 1;
        ulong sloppyPlayer = 3;
        ulong victim = 2;

        statTracker.RegisterPlayer(sloppyPlayer);

        // Sloppy player misses between every shot and dies between every kill
        statTracker.RecordDamage(sloppyPlayer, victim, 50f);
        statTracker.RecordMiss(sloppyPlayer);
        statTracker.RecordDamage(sloppyPlayer, victim, 50f);
        statTracker.RecordMiss(sloppyPlayer);
        statTracker.RecordDamage(sloppyPlayer, victim, 50f);
        statTracker.RecordMiss(sloppyPlayer);
        statTracker.RecordKill(sloppyPlayer, victim);
        statTracker.RecordDeath(sloppyPlayer);
        statTracker.RecordKill(sloppyPlayer, victim);
        statTracker.RecordDeath(sloppyPlayer);
        statTracker.RecordKill(sloppyPlayer, victim);

        // Clean player never misses and never dies
        statTracker.RecordDamage(cleanPlayer, victim, 50f);
        statTracker.RecordDamage(cleanPlayer, victim, 50f);
        statTracker.RecordDamage(cleanPlayer, victim, 50f);
        statTracker.RecordKill(cleanPlayer, victim);
        statTracker.RecordKill(cleanPlayer, victim);
        statTracker.RecordKill(cleanPlayer, victim);

        var sloppyStats = statTracker.GetStats(sloppyPlayer);
        var cleanStats = statTracker.GetStats(cleanPlayer);

        // Sloppy: 3x (50 * 1.5) + 3x (100 * 1) = 225 + 300 = 525
        // Clean: (50*1.5) + (50*2.0) + (50*2.5) + (100*1) + (100*2) + (100*3) = 300 + 600 = 900
        Assert.Greater(cleanStats.rating, sloppyStats.rating);
        Assert.Greater(cleanStats.rating - sloppyStats.rating, 200f);
    }
}