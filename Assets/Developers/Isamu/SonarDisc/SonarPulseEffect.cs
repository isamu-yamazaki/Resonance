using UnityEngine;

namespace Resonance.Abilities.SonarDisc
{
    /// <summary>
    /// Spawns a large sphere and drives the SonarPulse depth-intersection shader,
    /// creating a ring that appears wherever the expanding pulse intersects world geometry.
    /// Only expands in the hemisphere facing away from the wall the disc is attached to.
    /// </summary>
    public class SonarPulseEffect : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Material sonarPulseMaterial;
        [SerializeField] private float expandDuration = 0.6f;
        [SerializeField] private float maxRadius = 30f;

        public void Play()
        {
            if (sonarPulseMaterial == null)
            {
                Debug.LogWarning("[SonarPulseEffect] sonarPulseMaterial is not assigned.");
                return;
            }

            GameObject runner = new GameObject("SonarPulseRunner");
            SonarPulseRunner pulseRunner = runner.AddComponent<SonarPulseRunner>();
            pulseRunner.Initialize(transform.position, -transform.forward, sonarPulseMaterial, maxRadius, expandDuration);
        }
    }
}
