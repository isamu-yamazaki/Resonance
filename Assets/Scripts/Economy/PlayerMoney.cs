using PurrNet;
using UnityEngine;

namespace Resonance.Economy
{
    public class PlayerMoney : NetworkBehaviour
    {
        public static PlayerMoney Instance { get; private set; }

        [SerializeField] private float startingBalance = 50000f;
        public float Balance { get; private set; }

        public event System.Action<float> OnBalanceChanged;

        protected override void OnSpawned(bool asServer)
        {
            if (!isOwner) return;

            Instance = this;
            Balance = startingBalance;
        }

        public bool CanAfford(float cost) => Balance >= cost;

        public bool TrySpend(float cost)
        {
            if (!CanAfford(cost)) return false;
            Balance -= cost;
            OnBalanceChanged?.Invoke(Balance);
            return true;
        }

        public void AddFunds(float amount)
        {
            Balance += amount;
            OnBalanceChanged?.Invoke(Balance);
        }
    }
}