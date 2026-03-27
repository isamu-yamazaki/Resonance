using Resonance.Player;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Combat.Augments
{
    public class AbilitySprintBurst : MonoBehaviour, IAugmentAbility
    {
        [SerializeField] private float maxMeter = 15f;
        private float currentMeter;

        public string AbilityKey => "ability_sprintBurst";
        public string Name => "Sprint Burst";
        public string Description => "Move with a brief burst of speed.";
        public float MaxCooldown => maxMeter;
        public float CurrentCooldown
        {
            get => currentMeter;
            set => currentMeter = Mathf.Clamp(value, 0f, maxMeter);
        }
        public bool AbilityReady => false;

        public void ActivateAbility() { }

        private void Update()
        {
            // your sprint logic here
        }
    }
}