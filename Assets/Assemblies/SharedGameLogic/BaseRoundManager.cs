using System;
using System.Threading.Tasks;
using Resonance.Assemblies.MatchStat;

namespace Resonance.Assemblies.SharedGameLogic
{
    public abstract class BaseRoundManager : IRoundManager
    {
        protected BaseMatchState matchState = BaseMatchState.Waiting;
        protected MatchStatTracker matchStatTracker;
        protected float matchStartCountdownSeconds;

        public event Action<BaseMatchState, BaseMatchState> OnMatchStateChange;
        public event Action<float> OnMatchCountdownStart;
        public event Action OnMatchStart;

        public BaseMatchState MatchState => matchState;
        public bool IsMatchActive => matchState == BaseMatchState.MatchActive;
        public bool IsMatchEnded => matchState == BaseMatchState.MatchEnded;
        public float MatchStartCountdownSeconds => matchStartCountdownSeconds;

        protected BaseRoundManager(MatchStatTracker tracker, float countdownSeconds)
        {
            matchStatTracker = tracker;
            matchStartCountdownSeconds = countdownSeconds;
        }

        protected void RaiseMatchStart() => OnMatchStart?.Invoke();
        protected void RaiseMatchStateChange(BaseMatchState old, BaseMatchState @new)
            => OnMatchStateChange?.Invoke(old, @new);

        public async Task StartMatchCountdown()
        {
            if (matchState == BaseMatchState.MatchActive || matchState == BaseMatchState.Countdown)
            {
                return;
            }

            var oldMatchState = matchState;
            matchState = BaseMatchState.Countdown;

            OnMatchCountdownStart?.Invoke(matchStartCountdownSeconds);
            RaiseMatchStateChange(oldMatchState, matchState);

            await Task.Delay((int)(matchStartCountdownSeconds * 1000));
            StartMatchWithoutCountdown();
        }

        public abstract void StartMatchWithoutCountdown();
    }
}
