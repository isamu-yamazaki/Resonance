using UnityEngine;

namespace Resonance.Combat.Augments
{
    public interface IAugmentAbility
    {
        public string Name { get; }
        public string Description { get; }

        public float Cooldown { get; set; }
        
        
    
        public void ActivateAbility();
    }
}
