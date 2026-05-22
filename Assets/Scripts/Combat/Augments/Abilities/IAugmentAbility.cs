using UnityEngine;

namespace Resonance.Combat.Augments
{
    public interface IAugmentAbility
    {
        string AbilityKey { get; }
        string Name { get; }
        string Description { get; }
        float MaxCooldown { get; }
        float CurrentCooldown { get; set; }
        bool AbilityReady { get; }

        void ActivateAbilityExternal();
        void SimulateActivateAbility();
    }
}
