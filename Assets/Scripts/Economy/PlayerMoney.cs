using PurrNet;
using PurrNet.Prediction;
using UnityEngine;

namespace Resonance.Economy
{
    public class PlayerMoney : PredictedIdentity<PlayerMoneyInput, PlayerMoneyState>
    {
        public static PlayerMoney LocalInstance { get; private set; }

        [SerializeField] private float startingBalance = 50000f;
        public float Balance => currentState.Balance;

        public event System.Action<float> OnBalanceChanged;

        private float pendingAmountToChange = 0;
        private PlayerMoneyState? previousVerifiedState;

        protected override void LateAwake()
        {
            if (isOwner)
                LocalInstance = this;
        }


        protected override PlayerMoneyState GetInitialState()
        {
            return new()
            {
                Balance = startingBalance
            };
        }

        public bool CanAfford(float cost) => Balance >= cost;

        public bool TrySpend(float cost)
        {
            if (!CanAfford(cost)) return false;
            pendingAmountToChange -= cost;
            return true;
        }

        public void AddFunds(float amount)
        {
            pendingAmountToChange += amount;
        }

        protected override void GetFinalInput(ref PlayerMoneyInput input)
        {
            input.AmountToChange = pendingAmountToChange;
            pendingAmountToChange = 0;
        }

        protected override void Simulate(PlayerMoneyInput input, ref PlayerMoneyState state, float delta)
        {
            state.Balance += input.AmountToChange;
        }

        protected override void UpdateView(PlayerMoneyState viewState, PlayerMoneyState? verified)
        {
            if (!verified.HasValue) return;
            var v = verified.Value;

            if (!previousVerifiedState.HasValue || previousVerifiedState.Value.Balance != v.Balance)
                OnBalanceChanged?.Invoke(v.Balance);

            previousVerifiedState = v;
        }
    }
}
