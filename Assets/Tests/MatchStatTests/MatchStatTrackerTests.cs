using System.Collections.Generic;
using NUnit.Framework;
using Resonance.Assemblies.MatchStat;

public class MatchStatTrackerTests
{
    private ulong expectedKillerId = 1;
    private ulong expectedVictimId = 2;

    private PlayerMatchStats expectedStatsAfterOneKill = new()
    {
        kills = 1,
        killStreak = 1,
        bestKillStreak = 1,
    };

    private PlayerMatchStats expectedStatsAfterOneDeath = new()
    {
        deaths = 1,
    };

    private MatchStatTracker tracker;

    [SetUp]
    public void Setup()
    {
        tracker = new MatchStatTracker();
    }

    #region RecordDamage
    [Test]
    public void RecordDamage_UpdatesTotalDamageDealtInStats()
    {
        tracker.RecordDamage(expectedKillerId, expectedVictimId, 5);
        tracker.RecordDamage(expectedKillerId, 3, 10);
        tracker.RecordDamage(expectedKillerId, 4, 500);
        tracker.RecordDamage(5, 6, 20);

        var stats = tracker.GetStats(expectedKillerId);
        Assert.AreEqual(515, stats.totalDamageDealt);
    }


    #endregion

    #region RecordKill
    [Test]
    public void RecordKill_FiresOnKillEvent()
    {
        ulong OnPlayerKill_killerCaptured = default;
        ulong OnPlayerKill_victimCaptured = default;

        tracker.OnPlayerKill += (killer, victim) =>
        {
            OnPlayerKill_killerCaptured = killer;
            OnPlayerKill_victimCaptured = victim;
        };


        tracker.RecordKill(expectedKillerId, expectedVictimId);

        // Expect killer stats to have been updated before victim stats
        Assert.AreEqual(expectedKillerId, OnPlayerKill_killerCaptured);
        Assert.AreEqual(expectedVictimId, OnPlayerKill_victimCaptured);
    }

    [Test]
    public void RecordKill_FiresStatsUpdateEvent()
    {
        ulong[] OnStatsUpdate_capturedPlayers = { 0, 0 };
        PlayerMatchStats[] OnStatsUpdate_stats = { new(), new() };
        int i = 0;

        tracker.OnStatsUpdated += (player, stats) =>
        {
            OnStatsUpdate_capturedPlayers[i] = player;
            OnStatsUpdate_stats[i] = stats;
            i += 1;
        };

        tracker.RecordKill(expectedKillerId, expectedVictimId);

        // killer, then victim
        ulong[] expectedCapturedPlayers = { 1, 2 };
        PlayerMatchStats[] expectedCapturedStats =
        {
            expectedStatsAfterOneKill,
            expectedStatsAfterOneDeath,
        };
        Assert.AreEqual(expectedCapturedPlayers, OnStatsUpdate_capturedPlayers);
    }

    [Test]
    public void RecordKill_UpdatesPlayerStats()
    {
        tracker.RecordKill(expectedKillerId, expectedVictimId);

        var allStats = tracker.GetAllStats();
        Assert.AreEqual(expectedStatsAfterOneKill, allStats[expectedKillerId]);
        Assert.AreEqual(expectedStatsAfterOneDeath, allStats[expectedVictimId]);
    }

    [Test]
    public void RecordKill_ProcessesAssistsIfAboveDamageThreshold()
    {
        var config = new MatchStatTracker.MatchStatTrackerConfig
        {
            assistTimeWindowMs = 100f,
            assistDamageThreshold = 20f
        };
        tracker = new MatchStatTracker(config);
        ulong[] expectedAssistIds = { 3, 4 };

        foreach (var id in expectedAssistIds)
        {
            // test out total damage before death
            tracker.RecordDamage(id, expectedVictimId, 15);
            tracker.RecordDamage(id, expectedVictimId, 15);
        }

        tracker.RecordKill(expectedKillerId, expectedVictimId);

        foreach (var id in expectedAssistIds)
        {
            var recordedStats = tracker.GetStats(id);
            Assert.AreEqual(1, recordedStats.assists);
        }
    }

    #endregion

    #region RecordDeath
    [Test]
    public void RecordDeath_UpdatesPlayerStats()
    {
        tracker.RecordDeath(expectedVictimId);
        var stats = tracker.GetStats(expectedVictimId);
        Assert.AreEqual(1, stats.deaths);
    }

    [Test]
    public void RecordDeath_FiresOnStatsUpdatedEvent()
    {
        ulong capturedPlayerId = 0;
        PlayerMatchStats capturedStats = new();

        tracker.OnStatsUpdated += (playerId, stats) =>
        {
            capturedPlayerId = playerId;
            capturedStats = stats;
        };

        tracker.RecordDeath(expectedVictimId);

        Assert.AreEqual(expectedVictimId, capturedPlayerId);
        Assert.AreEqual(1, capturedStats.deaths);
    }

    [Test]
    public void RecordDeath_FiresOnAllStatsUpdatedEvent()
    {
        Dictionary<ulong, PlayerMatchStats> capturedAllStats = null;

        tracker.OnAllStatsUpdated += (allStats) =>
        {
            capturedAllStats = allStats;
        };

        tracker.RecordDeath(expectedVictimId);

        Assert.IsNotNull(capturedAllStats);
    }
    #endregion

    #region GetKDA
    [Test]
    public void GetKDA_CorrectlyCalculatesForZeroDeaths()
    {
        // yo, this player is cracked
        tracker.RecordKill(expectedKillerId, expectedVictimId);
        tracker.RecordKill(expectedKillerId, expectedVictimId);
        tracker.RecordKill(expectedKillerId, expectedVictimId);

        tracker.RecordDamage(expectedKillerId, expectedVictimId, 50);
        tracker.RecordKill(3, expectedVictimId);

        var kda = tracker.GetKDA(expectedKillerId);
        Assert.AreEqual(4f, kda);
    }

    [Test]
    public void GetKDA_CorrectlyCalculatesWithDeaths()
    {
        tracker.RecordKill(expectedKillerId, expectedVictimId);
        tracker.RecordKill(expectedVictimId, expectedKillerId);
        tracker.RecordKill(expectedKillerId, expectedVictimId);

        // apparently assists are supposed to affect KDA?
        tracker.RecordDamage(expectedKillerId, expectedVictimId, 50);
        tracker.RecordKill(3, expectedVictimId);

        tracker.RecordKill(expectedVictimId, expectedKillerId);

        var kda = tracker.GetKDA(expectedKillerId);
        Assert.AreEqual(1.5f, kda);
    }

    #endregion

    #region ResetAllStats
    [Test]
    public void ResetAllStats_FiresOnAllStatsUpdated()
    {
        int eventCallCount = 0;
        Dictionary<ulong, PlayerMatchStats> capturedStats = null;

        // Set up multiple players with stats
        tracker.RecordKill(1, 2);
        tracker.RecordKill(3, 4);

        tracker.OnAllStatsUpdated += (allStats) =>
        {
            eventCallCount++;
            capturedStats = allStats;
        };

        tracker.ResetAllStats();

        // Event should fire exactly once after all stats are reset
        Assert.AreEqual(1, eventCallCount);
        Assert.IsNotNull(capturedStats);

        // Verify all stats in the dictionary are reset
        foreach (var kvp in capturedStats)
        {
            Assert.AreEqual(0, kvp.Value.kills);
            Assert.AreEqual(0, kvp.Value.deaths);
        }
    }

    [Test]
    public void ResetAllStats_ClearsEveryPlayer()
    {
        // Create multiple players with various stats
        tracker.RecordKill(1, 2);
        tracker.RecordKill(3, 4);
        tracker.RecordKill(1, 2);
        tracker.RecordDeath(5);

        tracker.ResetAllStats();

        var allStats = tracker.GetAllStats();

        // Verify all players have reset stats
        foreach (var (_, value) in allStats)
        {
            Assert.AreEqual(new PlayerMatchStats(), value);
        }
    }

    #endregion

    #region ResetPlayerStats
    [Test]
    public void ResetPlayerStats_ClearsPlayerStats()
    {
        ulong targetPlayerId = 1;
        ulong otherPlayerId = 3;

        // Create multiple players with stats
        tracker.RecordKill(targetPlayerId, 2);
        tracker.RecordKill(targetPlayerId, 2);
        tracker.RecordKill(otherPlayerId, 4);

        // Capture other player's stats before reset
        var otherStatsBeforeReset = tracker.GetStats(otherPlayerId);

        tracker.ResetPlayerStats(targetPlayerId);

        var targetStats = tracker.GetStats(targetPlayerId);
        var otherStats = tracker.GetStats(otherPlayerId);

        Assert.AreEqual(new PlayerMatchStats(), targetStats);
        Assert.AreEqual(otherStatsBeforeReset, otherStats);
        Assert.AreEqual(otherStatsBeforeReset, otherStats);
    }

    [Test]
    public void ResetPlayerStats_FiresOnAllStatsUpdated()
    {
        bool eventFired = false;
        Dictionary<ulong, PlayerMatchStats> capturedStats = null;

        tracker.RecordKill(1, 2);

        tracker.OnAllStatsUpdated += (allStats) =>
        {
            eventFired = true;
            capturedStats = allStats;
        };

        tracker.ResetPlayerStats(1);

        Assert.IsTrue(eventFired);
        Assert.IsNotNull(capturedStats);
        Assert.AreEqual(0, capturedStats[1].kills);
    }
    #endregion
}
