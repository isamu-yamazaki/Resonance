using Unity.Cinemachine;
using UnityEngine;

namespace Resonance.Combat
{
    public class MuzzleShaker : MonoBehaviour
    {
        [SerializeField] private CinemachineImpulseSource impulseSource;

        public void Shake()
        {
            if (impulseSource != null)
                impulseSource.GenerateImpulse();
        }
    }
}