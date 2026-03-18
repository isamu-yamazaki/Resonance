using System;
using System.Threading.Tasks;
using PurrNet;
using Resonance.Assemblies.MatchStat;
using Resonance.Assemblies.SharedGameLogic;
using UnityEngine;

namespace Resonance.Match
{
    /// <summary>
    /// Abstract NetworkModule base class that contains logic shared across all round manager
    /// network adapters: shared events, cached state, lifecycle hooks, and common RPCs.
    /// </summary>
    [Serializable]
    public abstract class BaseRoundManagerNetworkAdapter : NetworkModule
    {
        protected MatchStatNetworkAdapter matchStatNetworkAdapter;

        #region Cached Client-Side State
        protected BaseMatchState cachedMatchState;

        public bool IsMatchActive => cachedMatchState == BaseMatchState.MatchActive;
        public bool IsMatchEnded  => cachedMatchState == BaseMatchState.MatchEnded;
        #endregion

        #region Events
        public event Action<BaseMatchState, BaseMatchState> OnMatchStateChange;
        public event Action<float> OnMatchCountdownStart;
        public event Action OnMatchStart;
        #endregion

        #region Constructor
        protected BaseRoundManagerNetworkAdapter(MatchStatNetworkAdapter adapter)
        {
            matchStatNetworkAdapter = adapter;
            matchStatNetworkAdapter.OnMatchStatTrackerCreated.AddListener(OnMatchStatTrackerCreated);
        }
        #endregion

        #region Lifecycle
        public override void OnSpawn(bool asServer)
        {
            base.OnSpawn(asServer);
        }

        public override void OnDespawned(bool asServer)
        {
            base.OnDespawned(asServer);
            matchStatNetworkAdapter?.OnMatchStatTrackerCreated.RemoveListener(OnMatchStatTrackerCreated);
            if (asServer)
            {
                DestroyRoundManager();
            }
        }
        #endregion

        #region Initialization
        private void OnMatchStatTrackerCreated(MatchStatTracker tracker)
        {
            if (!HasRoundManager())
            {
                CreateRoundManager(tracker);
            }
            else
            {
                Debug.LogWarning($"[{GetType().Name}] MatchStatTracker received but round manager already exists; re-creating");
                DestroyRoundManager();
                CreateRoundManager(tracker);
            }
        }

        protected abstract bool HasRoundManager();
        protected abstract void CreateRoundManager(MatchStatTracker tracker);
        protected abstract void DestroyRoundManager();
        #endregion

        #region Protected Event Handlers (for use in subclass Create/Destroy)
        protected void HandleMatchCountdownStart(float countdownSeconds)
            => FireMatchCountdownStartObservers(countdownSeconds);

        protected void HandleMatchStateChange(BaseMatchState oldState, BaseMatchState newState)
            => FireMatchStateChangeObservers((int)oldState, (int)newState);
        #endregion

        #region Server to Client RPCs
        [ObserversRpc]
        protected void FireMatchCountdownStartObservers(float countdownSeconds)
        {
            Debug.Log($"[{GetType().Name}] Match countdown of {countdownSeconds}s started");
            OnMatchCountdownStart?.Invoke(countdownSeconds);
        }

        [ObserversRpc]
        protected void FireMatchStateChangeObservers(int oldState, int newState)
        {
            Debug.Log($"[{GetType().Name}] Match state changed from {oldState} to {newState}");
            cachedMatchState = (BaseMatchState)newState;
            OnMatchStateChange?.Invoke((BaseMatchState)oldState, (BaseMatchState)newState);
        }

        [ObserversRpc]
        protected void FireMatchStartObservers(int matchStartParam)
        {
            Debug.Log($"[{GetType().Name}] Match started, param: {matchStartParam}");
            CacheMatchStartParam(matchStartParam);
            OnMatchStart?.Invoke();
        }

        protected abstract void CacheMatchStartParam(int param);
        #endregion

        #region Client to Server Actions (Shared Public API)
        public void StartMatchCountdown()
        {
            Debug.Log($"[{GetType().Name}] StartMatchCountdown requested");
            StartMatchCountdown_Server();
        }

        [ServerRpc]
        private void StartMatchCountdown_Server() => CallStartMatchCountdown();

        protected abstract void CallStartMatchCountdown();
        #endregion

        #region Getters (Client Callable)
        [ServerRpc]
        public async Task<bool> GetIsMatchActive() => GetRoundManagerIsMatchActive();

        [ServerRpc]
        public async Task<bool> GetIsMatchEnded() => GetRoundManagerIsMatchEnded();

        protected abstract bool GetRoundManagerIsMatchActive();
        protected abstract bool GetRoundManagerIsMatchEnded();
        #endregion
    }
}
