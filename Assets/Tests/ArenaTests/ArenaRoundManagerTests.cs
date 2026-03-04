using NUnit.Framework;
using Resonance.Assemblies.MatchStat;
using Resonance.Assemblies.Arena;
using System.Threading.Tasks;

public class ArenaRoundManagerTests
{
    private MatchStatTracker statTracker;
    private ArenaRoundManager roundManager;

    [SetUp]
    public void Setup()
    {
        statTracker = new();
        roundManager = new(statTracker);
    }

    #region Properties
    [Test]
    public void SecondsRemainingForMatch_ReturnsZeroIfUnknownMatchStartTime()
    {
        Assert.AreEqual(0, roundManager.SecondsRemainingForMatch);
    }

    [Test]
    public void SecondsRemainingForMatch_ReturnsCalculatedTimeIfMatchRunning()
    {
        var config = ArenaRoundManager.ArenaRoundManagerConfig.Default;
        config.matchDurationSeconds = 120f;
        var roundManager = new ArenaRoundManager(statTracker, config);

        roundManager.StartMatchWithoutCountdown();

        Assert.GreaterOrEqual(roundManager.SecondsRemainingForMatch, 118f);
    }
    #endregion

    #region OnPlayerKilled
    [Test]
    public void OnPlayerKilled_UpdatesLeaderIfRoundStarted()
    {
        roundManager.StartMatchWithoutCountdown();

        ulong killerId = 1;

        statTracker.RecordKill(killerId, 2);
        statTracker.RecordKill(killerId, 3);

        Assert.AreEqual(killerId, roundManager.CurrentLeader);
    }

    [Test]
    public void OnPlayerKilled_DoesNotUpdateLeaderIfRoundNotActive()
    {
        ulong killerId = 1;

        statTracker.RecordKill(killerId, 2);
        statTracker.RecordKill(killerId, 3);

        Assert.IsNull(roundManager.CurrentLeader);
    }

    [Test]
    public void OnPlayerKilled_EndMatchIfEliminationThresholdPassed()
    {
        roundManager.StartMatchWithoutCountdown();
        ulong killerId = 1;

        for (int i = 0; i < roundManager.EliminationsToWin; i++)
        {
            statTracker.RecordKill(killerId, 2);
        }

        Assert.AreEqual(true, roundManager.IsMatchEnded);
    }
    #endregion

    #region EndMatch
    [Test]
    public async Task EndMatch_FiresOnMatchEndEvent()
    {
        roundManager.StartMatchWithoutCountdown();

        ulong? capturedWinner = null;
        int eventCallCount = 0;

        roundManager.OnMatchEnd += (winner) =>
        {
            capturedWinner = winner;
            eventCallCount++;
        };

        ulong expectedWinner = 1;
        await roundManager.EndMatch(expectedWinner);

        Assert.AreEqual(1, eventCallCount);
        Assert.AreEqual(expectedWinner, capturedWinner);
    }

    [Test]
    public async Task EndMatch_FiresMatchStateChangeEvent()
    {
        roundManager.StartMatchWithoutCountdown();

        ArenaMatchState capturedOldState = default;
        ArenaMatchState capturedNewState = default;
        int eventCallCount = 0;
        roundManager.OnMatchStateChange += (oldState, newState) =>
        {
            capturedOldState = oldState;
            capturedNewState = newState;
            eventCallCount++;
        };

        await roundManager.EndMatch(null);

        Assert.AreEqual(ArenaMatchState.MatchActive, capturedOldState);
        Assert.AreEqual(ArenaMatchState.MatchEnded, capturedNewState);
        Assert.AreEqual(1, eventCallCount);
    }

    [Test]
    public async Task EndMatch_UpdatesTheMatchStatus()
    {
        roundManager.StartMatchWithoutCountdown();
        await roundManager.EndMatch(1);

        Assert.AreEqual(false, roundManager.IsMatchActive);
        Assert.AreEqual(true, roundManager.IsMatchEnded);
    }


    [Test]
    public async Task EndMatch_AutoStartsNextRoundIfConfigured()
    {
        var config = new ArenaRoundManager.ArenaRoundManagerConfig
        {
            eliminationsToWin = 10,
            autoStartNextMatch = true,
            autoStartDelaySeconds = 1,
            matchStartCountdownSeconds = 5f,
            matchDurationSeconds = 300f,
        };
        var autoStartManager = new ArenaRoundManager(statTracker, config);
        autoStartManager.StartMatchWithoutCountdown();
        await autoStartManager.EndMatch(1);

        Assert.AreEqual(true, autoStartManager.IsMatchActive);
        Assert.AreEqual(false, autoStartManager.IsMatchEnded);
    }

    [Test]
    public async Task EndMatch_FiresMatchCountdownEventIfAutoStart()
    {
        var config = new ArenaRoundManager.ArenaRoundManagerConfig
        {
            eliminationsToWin = 10,
            autoStartNextMatch = true,
            autoStartDelaySeconds = 1,
            matchStartCountdownSeconds = 5f,
            matchDurationSeconds = 300f,
        };

        var autoStartManager = new ArenaRoundManager(statTracker, config);
        var eventCallCount = 0;
        autoStartManager.OnMatchCountdownStart += (seconds) =>
        {
            eventCallCount++;
        };

        autoStartManager.StartMatchWithoutCountdown();
        await autoStartManager.EndMatch(1);

        Assert.AreEqual(1, eventCallCount);
    }

    [Test]
    public async Task EndMatch_DoesNothingIfMatchNotActive()
    {
        int eventCallCount = 0;
        roundManager.OnMatchEnd += (_) => eventCallCount++;

        await roundManager.EndMatch(1);

        Assert.AreEqual(0, eventCallCount);
        Assert.AreEqual(false, roundManager.IsMatchActive);
        Assert.AreEqual(false, roundManager.IsMatchEnded);
    }
    #endregion

    #region StartMatchCountdown
    [Test]
    public async Task StartMatchCountdown_UpdatesMatchStateDuringCountdown()
    {
        _ = roundManager.StartMatchCountdown();
        await Task.Delay(1000);
        Assert.AreEqual(ArenaMatchState.Countdown, roundManager.MatchState);
    }

    [Test]
    public async Task StartMatchCountdown_FiresEventWithCountdownSeconds()
    {
        float capturedSeconds = 0;
        int eventCallCount = 0;
        roundManager.OnMatchCountdownStart += (seconds) =>
        {
            capturedSeconds = seconds;
            eventCallCount++; ;
        };

        _ = roundManager.StartMatchCountdown();
        await Task.Delay(1000);

        Assert.AreEqual(roundManager.MatchStartCountdownSeconds, capturedSeconds);
        Assert.AreEqual(1, eventCallCount);
    }



    [Test]
    public async Task StartMatchCountdown_FiresMatchStateChangeEvent()
    {
        ArenaMatchState capturedOldState = default;
        ArenaMatchState capturedNewState = default;
        int eventCallCount = 0;
        roundManager.OnMatchStateChange += (oldState, newState) =>
        {
            capturedOldState = oldState;
            capturedNewState = newState;
            eventCallCount++;
        };

        _ = roundManager.StartMatchCountdown();
        Assert.AreEqual(ArenaMatchState.Waiting, capturedOldState);
        Assert.AreEqual(ArenaMatchState.Countdown, capturedNewState);
        Assert.AreEqual(1, eventCallCount);
    }

    [Test]
    public async Task StartMatchCountdown_ActuallyStartsTheMatch()
    {
        roundManager = new(statTracker, new()
        {
            matchStartCountdownSeconds = 0.5f,
        });

        await roundManager.StartMatchCountdown();
        Assert.AreEqual(ArenaMatchState.MatchActive, roundManager.MatchState);
    }

    #endregion

    #region StartMatchWithoutCountdown
    [Test]
    public void StartMatchWithoutCountdown_UpdatesTheMatchStatus()
    {
        roundManager.StartMatchWithoutCountdown();

        Assert.AreEqual(true, roundManager.IsMatchActive);
        Assert.AreEqual(false, roundManager.IsMatchEnded);
    }


    [Test]
    public void StartMatchWithoutCountdown_FiresOnMatchStartEvent()
    {
        int eventCallCount = 0;
        roundManager.OnMatchStart += () => eventCallCount++;

        roundManager.StartMatchWithoutCountdown();

        Assert.AreEqual(1, eventCallCount);
    }

    [Test]
    public void StartMatchWithoutCountdown_FiresMatchStateChangeEvent()
    {
        ArenaMatchState capturedOldState = default;
        ArenaMatchState capturedNewState = default;
        int eventCallCount = 0;
        roundManager.OnMatchStateChange += (oldState, newState) =>
        {
            capturedOldState = oldState;
            capturedNewState = newState;
            eventCallCount++;
        };

        roundManager.StartMatchWithoutCountdown();
        Assert.AreEqual(ArenaMatchState.Waiting, capturedOldState);
        Assert.AreEqual(ArenaMatchState.MatchActive, capturedNewState);
        Assert.AreEqual(1, eventCallCount);

    }

    [Test]
    public void StartMatchWithoutCountdown_DoesNothingIfMatchAlreadyActive()
    {
        roundManager.StartMatchWithoutCountdown();

        int eventCallCount = 0;
        roundManager.OnMatchStart += () => eventCallCount++;

        roundManager.StartMatchWithoutCountdown();

        Assert.AreEqual(0, eventCallCount);
        Assert.AreEqual(true, roundManager.IsMatchActive);
    }

    [Test]
    public void StartMatchWithoutCountdown_ResetsMatchStatTracker()
    {
        statTracker.RecordKill(1, 2);
        statTracker.RecordKill(3, 4);

        roundManager.StartMatchWithoutCountdown();

        var allStats = statTracker.GetAllStats();
        foreach (var kvp in allStats)
        {
            Assert.AreEqual(new PlayerMatchStats(), kvp.Value);
        }
    }

    [Test]
    public async Task StartMatchWithoutCountdown_LoopsMatchEndCheck()
    {
        var roundManager = new ArenaRoundManager(statTracker, new()
        {
            autoStartNextMatch = false,
            eliminationsToWin = 10,
            autoStartDelaySeconds = 1f,
            matchStartCountdownSeconds = 1f,
            matchDurationSeconds = 0.5f,
        });

        double capturedSecondsRemaining = 0;
        var eventCallCount = 0;
        roundManager.OnMatchTimerElapsed += (secondsRemaining) =>
        {
            capturedSecondsRemaining = secondsRemaining;
            eventCallCount++;
        };

        roundManager.StartMatchWithoutCountdown();

        await Task.Delay(2000);

        Assert.AreEqual(ArenaMatchState.MatchEnded, roundManager.MatchState);
        Assert.LessOrEqual(eventCallCount, 1);
    }

    #endregion


    #region GetLeaderboard
    [Test]
    public void GetLeaderboard_GetsAllPlayerRankingsDependentOnStats()
    {
        statTracker.RecordKill(1, 2);
        statTracker.RecordKill(1, 2);
        statTracker.RecordKill(1, 2);
        statTracker.RecordKill(3, 2);
        statTracker.RecordKill(3, 2);
        statTracker.RecordKill(4, 2);

        var leaderboard = roundManager.GetLeaderboard();

        Assert.AreEqual(4, leaderboard.Count);
        Assert.AreEqual((ulong)1, leaderboard[0].player);
        Assert.AreEqual((ulong)3, leaderboard[1].player);
        Assert.AreEqual((ulong)4, leaderboard[2].player);
        Assert.AreEqual((ulong)2, leaderboard[3].player);
    }
    #endregion
}
