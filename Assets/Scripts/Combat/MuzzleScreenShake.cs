using Unity.Cinemachine;
using UnityEngine;

namespace Resonance.Combat
{
    public class MuzzleScreenShake : MonoBehaviour
    {
        [SerializeField] private CinemachineImpulseSource impulseSource;

        public void Shake()
        {
            if (impulseSource != null)
                impulseSource.GenerateImpulse();
        }
    }
}