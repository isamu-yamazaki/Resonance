using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Resonance.Assemblies.MatchStat;
using Resonance.Assemblies.SharedGameLogic;

public class BaseRoundManagerTests
{
    private MatchStatTracker statTracker;
    private TestRoundManager roundManager;

    private class TestRoundManager : BaseRoundManager
    {
        public TestRoundManager(MatchStatTracker tracker, float countdown)
            : base(tracker, countdown)
        {
        }

        public override void StartMatchWithoutCountdown()
        {
            if (IsMatchActive) { return; }

            var oldMatchState = matchState;
            matchState = BaseMatchState.MatchActive;

            RaiseMatchStart();
            RaiseMatchStateChange(oldMatchState, matchState);
        }
    }

    [SetUp]
    public void Setup()
    {
        statTracker = new();
        roundManager = new(statTracker, 5f);
    }

    #region Properties
    [Test]
    public void IsMatchActive_ReturnsFalseByDefault()
    {
        Assert.IsFalse(roundManager.IsMatchActive);
    }

    [Test]
    public void IsMatchEnded_ReturnsFalseByDefault()
    {
        Assert.IsFalse(roundManager.IsMatchEnded);
    }

    [Test]
    public void MatchStartCountdownSeconds_ReturnsConfiguredValue()
    {
        Assert.AreEqual(5f, roundManager.MatchStartCountdownSeconds);
    }
    #endregion

    #region StartMatchCountdown
    [Test]
    public void StartMatchCountdown_SetsStateToCountdown()
    {
        _ = roundManager.StartMatchCountdown();

        Assert.AreEqual(BaseMatchState.Countdown, roundManager.MatchState);
    }

    [Test]
    public void StartMatchCountdown_FiresOnMatchCountdownStart()
    {
        float capturedSeconds = 0;
        int eventCallCount = 0;
        roundManager.OnMatchCountdownStart += (seconds) =>
        {
            capturedSeconds = seconds;
            eventCallCount++;
        };

        _ = roundManager.StartMatchCountdown();

        Assert.AreEqual(1, eventCallCount);
        Assert.AreEqual(5f, capturedSeconds);
    }

    [Test]
    public void StartMatchCountdown_FiresOnMatchStateChangeToCountdown()
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
    public void StartMatchCountdown_DoesNothingIfAlreadyActive()
    {
        roundManager.StartMatchWithoutCountdown();

        int eventCallCount = 0;
        roundManager.OnMatchCountdownStart += (_) => eventCallCount++;

        _ = roundManager.StartMatchCountdown();

        Assert.AreEqual(0, eventCallCount);
    }

    [Test]
    public void StartMatchCountdown_DoesNothingIfAlreadyInCountdown()
    {
        _ = roundManager.StartMatchCountdown();

        int eventCallCount = 0;
        roundManager.OnMatchCountdownStart += (_) => eventCallCount++;

        _ = roundManager.StartMatchCountdown();

        Assert.AreEqual(0, eventCallCount);
    }

    [Test]
    public async Task StartMatchCountdown_CallsStartMatchWithoutCountdownAfterDelay()
    {
        roundManager = new(statTracker, 0.1f);

        await roundManager.StartMatchCountdown();

        Assert.IsTrue(roundManager.IsMatchActive);
    }
    #endregion
}
