using UnityEngine;

namespace Resonance.Combat.Augments
{
    public interface IAugmentAbility
    {
        public string Name { get; }
        public string Description { get; }
    
        public void ActivateAbility();
    }
}
