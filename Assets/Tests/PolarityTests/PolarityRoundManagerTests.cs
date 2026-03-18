using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Resonance.Assemblies.MatchStat;
using Resonance.Assemblies.Polarity;
using Resonance.Assemblies.SharedGameLogic;

public class PolarityRoundManagerTests
{
    private MatchStatTracker statTracker;
    private PolarityRoundManager roundManager;

    [SetUp]
    public void Setup()
    {
        statTracker = new();
        roundManager = new(statTracker);
    }

    private PolarityRoundManager CreateManagerWithConfig(
        int timeBetweenRoleSwitchSeconds = 90,
        int teamEliminationsToWin = 10,
        float matchStartCountdownSeconds = 5f)
    {
        return new PolarityRoundManager(statTracker, new()
        {
            timeBetweenRoleSwitchSeconds = timeBetweenRoleSwitchSeconds,
            teamEliminationsToWin = teamEliminationsToWin,
            matchStartCountdownSeconds = matchStartCountdownSeconds,
        });
    }

    private void SetupTwoPlayerTeams(PolarityRoundManager manager)
    {
        manager.RegisterPlayersForTeamA(new() { 1 });
        manager.RegisterPlayersForTeamB(new() { 2 });
    }

    #region Properties
    [Test]
    public void MatchState_DefaultsToWaiting()
    {
        Assert.AreEqual(BaseMatchState.Waiting, roundManager.MatchState);
    }

    [Test]
    public void IsMatchActive_ReturnsFalseByDefault()
    {
        Assert.AreEqual(false, roundManager.IsMatchActive);
    }

    [Test]
    public void IsMatchEnded_ReturnsFalseByDefault()
    {
        Assert.AreEqual(false, roundManager.IsMatchEnded);
    }

    [Test]
    public void TeamEliminationsToWin_ReturnsConfiguredValue()
    {
        Assert.AreEqual(10, roundManager.TeamEliminationsToWin);
    }

    [Test]
    public void MatchStartCountdownSeconds_ReturnsConfiguredValue()
    {
        Assert.AreEqual(5f, roundManager.MatchStartCountdownSeconds);
    }

    [Test]
    public void TeamA_DefaultsToTaggersRole()
    {
        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Taggers, roundManager.GetTeam(TeamId.TeamA).currentRole);
    }

    [Test]
    public void TeamB_DefaultsToRunnersRole()
    {
        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Runners, roundManager.GetTeam(TeamId.TeamB).currentRole);
    }

    [Test]
    public void TeamA_HasEmptyPlayersSetByDefault()
    {
        Assert.IsNotNull(roundManager.GetPlayersForTeam(TeamId.TeamA));
        Assert.AreEqual(0, roundManager.GetPlayersForTeam(TeamId.TeamA).Count);
    }

    [Test]
    public void TeamB_HasEmptyPlayersSetByDefault()
    {
        Assert.IsNotNull(roundManager.GetPlayersForTeam(TeamId.TeamB));
        Assert.AreEqual(0, roundManager.GetPlayersForTeam(TeamId.TeamB).Count);
    }

    [Test]
    public void TeamAEliminations_DefaultsToZero()
    {
        Assert.AreEqual(0, roundManager.GetTeam(TeamId.TeamA).eliminations);
    }

    [Test]
    public void TeamBEliminations_DefaultsToZero()
    {
        Assert.AreEqual(0, roundManager.GetTeam(TeamId.TeamB).eliminations);
    }

    [Test]
    public void SecondsUntilNextRoleSwitch_ReturnsZeroIfMatchNotActive()
    {
        Assert.AreEqual(0, roundManager.SecondsUntilNextRoleSwitch);
    }
    #endregion

    #region RegisterPlayers
    [Test]
    public void RegisterPlayersForTeamA_UpdatesPlayersSet()
    {
        roundManager.RegisterPlayersForTeamA(new() { 1, 2, 3, 4 });
        Assert.AreEqual(new HashSet<ulong>() { 1, 2, 3, 4 }, roundManager.GetPlayersForTeam(TeamId.TeamA));
    }

    [Test]
    public void RegisterPlayersForTeamB_UpdatesPlayersSet()
    {
        roundManager.RegisterPlayersForTeamB(new() { 1, 2, 3, 4 });
        Assert.AreEqual(new HashSet<ulong>() { 1, 2, 3, 4 }, roundManager.GetPlayersForTeam(TeamId.TeamB));
    }
    #endregion

    #region StartMatchWithoutCountdown
    [Test]
    public void StartMatchWithoutCountdown_UpdatesMatchState()
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
        BaseMatchState capturedOldState = default;
        BaseMatchState capturedNewState = default;
        int eventCallCount = 0;
        roundManager.OnMatchStateChange += (oldState, newState) =>
        {
            capturedOldState = oldState;
            capturedNewState = newState;
            eventCallCount++;
        };

        roundManager.StartMatchWithoutCountdown();

        Assert.AreEqual(BaseMatchState.Waiting, capturedOldState);
        Assert.AreEqual(BaseMatchState.MatchActive, capturedNewState);
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
    public void StartMatchWithoutCountdown_ResetsTeamEliminations()
    {
        roundManager.StartMatchWithoutCountdown();

        Assert.AreEqual(0, roundManager.GetTeam(TeamId.TeamA).eliminations);
        Assert.AreEqual(0, roundManager.GetTeam(TeamId.TeamB).eliminations);
    }

    [Test]
    public void StartMatchWithoutCountdown_ResetsTeamRolesToDefault()
    {
        roundManager.StartMatchWithoutCountdown();

        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Taggers, roundManager.GetTeam(TeamId.TeamA).currentRole);
        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Runners, roundManager.GetTeam(TeamId.TeamB).currentRole);
    }

    [Test]
    public void StartMatchWithoutCountdown_RecordsTimeOfLastRoleSwitch()
    {
        roundManager.StartMatchWithoutCountdown();

        Assert.IsNotNull(roundManager.TimeOfLastRoleSwitch);
    }
    #endregion

    #region SwitchRoles
    [Test]
    public void SwitchRoles_SwapsTeamRoles()
    {
        roundManager.StartMatchWithoutCountdown();

        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Taggers, roundManager.GetTeam(TeamId.TeamA).currentRole);
        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Runners, roundManager.GetTeam(TeamId.TeamB).currentRole);

        roundManager.SwitchRoles();

        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Runners, roundManager.GetTeam(TeamId.TeamA).currentRole);
        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Taggers, roundManager.GetTeam(TeamId.TeamB).currentRole);
    }

    [Test]
    public void SwitchRoles_FiresOnRoleSwitchEvent()
    {
        roundManager.StartMatchWithoutCountdown();

        int eventCallCount = 0;
        roundManager.OnRoleSwitch += () => eventCallCount++;

        roundManager.SwitchRoles();

        Assert.AreEqual(1, eventCallCount);
    }

    [Test]
    public void SwitchRoles_DoesNothingIfMatchNotActive()
    {
        int eventCallCount = 0;
        roundManager.OnRoleSwitch += () => eventCallCount++;

        roundManager.SwitchRoles();

        Assert.AreEqual(0, eventCallCount);
        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Taggers, roundManager.GetTeam(TeamId.TeamA).currentRole);
    }

    [Test]
    public void SwitchRoles_DoubleSwitchRestoresOriginalRoles()
    {
        roundManager.StartMatchWithoutCountdown();

        roundManager.SwitchRoles();
        roundManager.SwitchRoles();

        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Taggers, roundManager.GetTeam(TeamId.TeamA).currentRole);
        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Runners, roundManager.GetTeam(TeamId.TeamB).currentRole);
    }

    [Test]
    public async Task RoleSwitchTimer_SwitchesRolesAfterConfiguredTime()
    {
        var manager = CreateManagerWithConfig(timeBetweenRoleSwitchSeconds: 2);
        manager.StartMatchWithoutCountdown();
        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Taggers, manager.GetTeam(TeamId.TeamA).currentRole);

        await Task.Delay(3000);

        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Runners, manager.GetTeam(TeamId.TeamA).currentRole);
        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Taggers, manager.GetTeam(TeamId.TeamB).currentRole);
    }

    [Test]
    public async Task RoleSwitchTimer_FiresOnRoleSwitchTimerElapsedEvent()
    {
        var manager = CreateManagerWithConfig(timeBetweenRoleSwitchSeconds: 4);

        int eventCallCount = 0;
        manager.OnRoleSwitchTimerElapsed += (_) => eventCallCount++;

        manager.StartMatchWithoutCountdown();

        await Task.Delay(2500);

        Assert.GreaterOrEqual(eventCallCount, 2);
    }
    #endregion

    #region EndMatch
    [Test]
    public async Task EndMatch_UpdatesMatchState()
    {
        roundManager.StartMatchWithoutCountdown();
        await roundManager.EndMatch(TeamId.TeamA);

        Assert.AreEqual(false, roundManager.IsMatchActive);
        Assert.AreEqual(true, roundManager.IsMatchEnded);
    }

    [Test]
    public async Task EndMatch_FiresOnMatchEndEvent()
    {
        roundManager.StartMatchWithoutCountdown();

        TeamId? capturedWinnerId = null;
        int eventCallCount = 0;
        roundManager.OnMatchEnd += (winnerId) =>
        {
            capturedWinnerId = winnerId;
            eventCallCount++;
        };

        await roundManager.EndMatch(TeamId.TeamA);

        Assert.AreEqual(1, eventCallCount);
        Assert.AreEqual(TeamId.TeamA, capturedWinnerId);
    }

    [Test]
    public async Task EndMatch_FiresMatchStateChangeEvent()
    {
        roundManager.StartMatchWithoutCountdown();

        BaseMatchState capturedOldState = default;
        BaseMatchState capturedNewState = default;
        int eventCallCount = 0;
        roundManager.OnMatchStateChange += (oldState, newState) =>
        {
            capturedOldState = oldState;
            capturedNewState = newState;
            eventCallCount++;
        };

        await roundManager.EndMatch(TeamId.TeamA);

        Assert.AreEqual(BaseMatchState.MatchActive, capturedOldState);
        Assert.AreEqual(BaseMatchState.MatchEnded, capturedNewState);
        Assert.AreEqual(1, eventCallCount);
    }

    [Test]
    public async Task EndMatch_StopsRoleSwitchTimer()
    {
        var manager = CreateManagerWithConfig(timeBetweenRoleSwitchSeconds: 1);
        manager.StartMatchWithoutCountdown();
        await manager.EndMatch(TeamId.TeamA);

        var roleAtEnd = manager.GetTeam(TeamId.TeamA).currentRole;
        await Task.Delay(1500);

        Assert.AreEqual(roleAtEnd, manager.GetTeam(TeamId.TeamA).currentRole);
    }

    [Test]
    public async Task EndMatch_DoesNothingIfMatchNotActive()
    {
        int eventCallCount = 0;
        roundManager.OnMatchEnd += (_) => eventCallCount++;

        await roundManager.EndMatch(TeamId.TeamA);

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
        Assert.AreEqual(BaseMatchState.Countdown, roundManager.MatchState);
    }

    [Test]
    public async Task StartMatchCountdown_FiresEventWithCountdownSeconds()
    {
        float capturedSeconds = 0;
        int eventCallCount = 0;
        roundManager.OnMatchCountdownStart += (seconds) =>
        {
            capturedSeconds = seconds;
            eventCallCount++;
        };

        _ = roundManager.StartMatchCountdown();
        await Task.Delay(1000);

        Assert.AreEqual(roundManager.MatchStartCountdownSeconds, capturedSeconds);
        Assert.AreEqual(1, eventCallCount);
    }

    [Test]
    public async Task StartMatchCountdown_FiresMatchStateChangeEvent()
    {
        BaseMatchState capturedOldState = default;
        BaseMatchState capturedNewState = default;
        int eventCallCount = 0;
        roundManager.OnMatchStateChange += (oldState, newState) =>
        {
            capturedOldState = oldState;
            capturedNewState = newState;
            eventCallCount++;
        };

        _ = roundManager.StartMatchCountdown();

        Assert.AreEqual(BaseMatchState.Waiting, capturedOldState);
        Assert.AreEqual(BaseMatchState.Countdown, capturedNewState);
        Assert.AreEqual(1, eventCallCount);
    }

    [Test]
    public async Task StartMatchCountdown_ActuallyStartsTheMatch()
    {
        roundManager = CreateManagerWithConfig(matchStartCountdownSeconds: 0.5f);

        await roundManager.StartMatchCountdown();
        Assert.AreEqual(BaseMatchState.MatchActive, roundManager.MatchState);
    }
    #endregion

    #region OnPlayerKilled
    [Test]
    public void OnPlayerKilled_DoesNothingIfMatchNotActive()
    {
        SetupTwoPlayerTeams(roundManager);

        statTracker.RecordKill(1, 2);

        Assert.AreEqual(0, roundManager.GetTeam(TeamId.TeamA).eliminations);
        Assert.AreEqual(0, roundManager.GetTeam(TeamId.TeamB).eliminations);
    }

    [Test]
    public void OnPlayerKilled_IncrementsKillerTeamEliminations()
    {
        SetupTwoPlayerTeams(roundManager);
        roundManager.StartMatchWithoutCountdown();

        statTracker.RecordKill(1, 2);
        statTracker.RecordKill(1, 2);
        statTracker.RecordKill(2, 1);

        Assert.AreEqual(2, roundManager.GetTeam(TeamId.TeamA).eliminations);
        Assert.AreEqual(1, roundManager.GetTeam(TeamId.TeamB).eliminations);
    }

    [Test]
    public void OnPlayerKilled_IncrementsCorrectTeamAfterRoleSwitch()
    {
        SetupTwoPlayerTeams(roundManager);
        roundManager.StartMatchWithoutCountdown();
        roundManager.SwitchRoles();

        statTracker.RecordKill(2, 1);
        statTracker.RecordKill(2, 1);

        Assert.AreEqual(0, roundManager.GetTeam(TeamId.TeamA).eliminations);
        Assert.AreEqual(2, roundManager.GetTeam(TeamId.TeamB).eliminations);
    }

    [Test]
    public void OnPlayerKilled_EndsMatchWhenTeamReachesEliminationThreshold()
    {
        var manager = CreateManagerWithConfig(teamEliminationsToWin: 3);
        SetupTwoPlayerTeams(manager);
        manager.StartMatchWithoutCountdown();

        statTracker.RecordKill(1, 2);
        statTracker.RecordKill(1, 2);
        statTracker.RecordKill(1, 2);

        Assert.AreEqual(true, manager.IsMatchEnded);
    }

    [Test]
    public void OnPlayerKilled_FiresOnMatchEndWithCorrectWinningTeam()
    {
        var manager = CreateManagerWithConfig(teamEliminationsToWin: 2);
        SetupTwoPlayerTeams(manager);
        manager.StartMatchWithoutCountdown();

        TeamId? capturedWinnerId = null;
        manager.OnMatchEnd += (winnerId) => capturedWinnerId = winnerId;

        statTracker.RecordKill(2, 1);
        statTracker.RecordKill(2, 1);

        Assert.IsNotNull(capturedWinnerId);
        Assert.AreEqual(TeamId.TeamB, capturedWinnerId);
    }

    [Test]
    public void OnPlayerKilled_DoesNotCountKillsFromUnassignedPlayers()
    {
        roundManager.RegisterPlayersForTeamA(new() { 1 });
        roundManager.StartMatchWithoutCountdown();

        statTracker.RecordKill(99, 1);
        statTracker.RecordKill(99, 1);
        statTracker.RecordKill(1, 99);

        Assert.AreEqual(1, roundManager.GetTeam(TeamId.TeamA).eliminations);
        Assert.AreEqual(0, roundManager.GetTeam(TeamId.TeamB).eliminations);
    }
    #endregion

    #region GetLeaderboard
    [Test]
    public void GetLeaderboard_ReturnsEmptyListsIfNoStats()
    {
        var leaderboard = roundManager.GetLeaderboard();

        Assert.IsNotNull(leaderboard);
        Assert.AreEqual(0, leaderboard[TeamId.TeamA].Count);
        Assert.AreEqual(0, leaderboard[TeamId.TeamB].Count);
    }

    [Test]
    public void GetLeaderboard_ReturnsPlayerRankingsForTeamA()
    {
        roundManager.RegisterPlayersForTeamA(new() { 1, 2 });
        roundManager.StartMatchWithoutCountdown();
        statTracker.RecordKill(1, 3);
        statTracker.RecordKill(2, 3);

        var leaderboard = roundManager.GetLeaderboard();

        Assert.AreEqual(2, leaderboard[TeamId.TeamA].Count);
    }

    [Test]
    public void GetLeaderboard_ReturnsPlayerRankingsForTeamB()
    {
        roundManager.RegisterPlayersForTeamB(new() { 3, 4 });
        roundManager.StartMatchWithoutCountdown();
        statTracker.RecordKill(3, 1);
        statTracker.RecordKill(4, 1);

        var leaderboard = roundManager.GetLeaderboard();

        Assert.AreEqual(2, leaderboard[TeamId.TeamB].Count);
    }

    [Test]
    public void GetLeaderboard_SortsPlayersByKillsDescending()
    {
        roundManager.RegisterPlayersForTeamA(new() { 1, 2, 3 });
        roundManager.StartMatchWithoutCountdown();
        statTracker.RecordKill(3, 99);
        statTracker.RecordKill(3, 99);
        statTracker.RecordKill(3, 99);
        statTracker.RecordKill(1, 99);
        statTracker.RecordKill(2, 99);
        statTracker.RecordKill(2, 99);

        var rankings = roundManager.GetLeaderboard()[TeamId.TeamA];

        Assert.AreEqual(3ul, rankings[0].player);
        Assert.AreEqual(2ul, rankings[1].player);
        Assert.AreEqual(1ul, rankings[2].player);
    }

    [Test]
    public void GetLeaderboard_TeamPlayersDoNotAppearInOppositeTeam()
    {
        roundManager.RegisterPlayersForTeamA(new() { 1, 2 });
        roundManager.RegisterPlayersForTeamB(new() { 3, 4 });
        roundManager.StartMatchWithoutCountdown();
        statTracker.RecordKill(1, 3);
        statTracker.RecordKill(3, 1);

        var leaderboard = roundManager.GetLeaderboard();
        var teamAIds = new HashSet<ulong>();
        var teamBIds = new HashSet<ulong>();
        foreach (var r in leaderboard[TeamId.TeamA]) teamAIds.Add(r.player);
        foreach (var r in leaderboard[TeamId.TeamB]) teamBIds.Add(r.player);

        Assert.IsFalse(teamAIds.Overlaps(teamBIds));
    }
    #endregion
}
