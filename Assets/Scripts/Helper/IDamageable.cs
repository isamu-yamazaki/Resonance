using UnityEngine;

namespace Resonance.Helper
{
    public interface IDamageable
    {
        void TakeDamage(float amount, GameObject source);
    }
}