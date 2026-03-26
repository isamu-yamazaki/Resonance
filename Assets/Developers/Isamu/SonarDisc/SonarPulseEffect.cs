using System.Collections;
using UnityEngine;

namespace Resonance.Abilities.SonarDisc
{
    /// <summary>
    /// Spawns an expanding sphere mesh driven by the SonarPulse shader.
    /// Attach to the disc prefab and call Play() when the pulse fires.
    /// </summary>
    public class SonarPulseEffect : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Material sonarPulseMaterial;
        [SerializeField] private float expandDuration = 0.6f;
        [SerializeField] private float maxRadius = 50f;

        private static readonly int PulseTimeID = Shader.PropertyToID("_PulseTime");

        public void Play()
        {
            StartCoroutine(PulseSequence());
        }

        private IEnumerator PulseSequence()
        {
            if (sonarPulseMaterial == null)
            {
                Debug.LogWarning("[SonarPulseEffect] sonarPulseMaterial is not assigned.");
                yield break;
            }

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = transform.position;

            // Remove collider so the sphere doesn't interfere with physics
            Destroy(sphere.GetComponent<Collider>());

            Material material = new Material(sonarPulseMaterial);
            sphere.GetComponent<MeshRenderer>().material = material;

            float elapsed = 0f;
            while (elapsed < expandDuration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / expandDuration);

                // Scale sphere outward
                float currentRadius = Mathf.Lerp(0f, maxRadius, normalizedTime);
                sphere.transform.localScale = Vector3.one * currentRadius * 2f;

                // Drive shader
                material.SetFloat(PulseTimeID, normalizedTime);

                yield return null;
            }

            Destroy(material);
            Destroy(sphere);
        }
    }
}
