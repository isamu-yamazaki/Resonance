using UnityEngine;

namespace Resonance.Combat.Augments
{
    public abstract class AugmentAbility : ScriptableObject
    {
        public abstract string Name { get; }

        public abstract string Description { get; }

        public abstract float CurrentCooldown { get; set; }
        
        public abstract bool AbilityReady { get; }

        public abstract void ActivateAbility();
    }
}
