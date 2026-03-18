using System;
using System.Threading.Tasks;

namespace Resonance.Assemblies.SharedGameLogic
{
    public interface IRoundManager
    {
        BaseMatchState MatchState { get; }
        bool IsMatchActive { get; }
        bool IsMatchEnded { get; }
        float MatchStartCountdownSeconds { get; }

        event Action<BaseMatchState, BaseMatchState> OnMatchStateChange;
        event Action<float> OnMatchCountdownStart;
        event Action OnMatchStart;

        Task StartMatchCountdown();
        void StartMatchWithoutCountdown();
    }
}
