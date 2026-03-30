using System.Collections;
using UnityEngine;

namespace Resonance.Abilities.SonarDisc
{
    /// <summary>
    /// Self-contained coroutine host for the sonar pulse VFX.
    /// Spawned by SonarPulseEffect so the effect survives disc destruction.
    /// </summary>
    public class SonarPulseRunner : MonoBehaviour
    {
        private static readonly int PulseTimeID     = Shader.PropertyToID("_PulseTime");
        private static readonly int DiscOriginID    = Shader.PropertyToID("_DiscOrigin");
        private static readonly int DiscForwardID   = Shader.PropertyToID("_DiscForward");
        private static readonly int CurrentRadiusID = Shader.PropertyToID("_CurrentRadius");

        public void Initialize(Vector3 origin, Vector3 forward, Material sourceMaterial, float maxRadius, float expandDuration)
        {
            StartCoroutine(PulseSequence(origin, forward, sourceMaterial, maxRadius, expandDuration));
        }

        private IEnumerator PulseSequence(Vector3 origin, Vector3 forward, Material sourceMaterial, float maxRadius, float expandDuration)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = origin;
            sphere.transform.localScale = Vector3.one * maxRadius * 2f;
            Destroy(sphere.GetComponent<Collider>());

            Material material = new Material(sourceMaterial);
            sphere.GetComponent<MeshRenderer>().material = material;

            float elapsed = 0f;
            while (elapsed < expandDuration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / expandDuration);
                float currentRadius  = Mathf.Lerp(0f, maxRadius, normalizedTime);

                material.SetFloat(PulseTimeID, normalizedTime);
                material.SetVector(DiscOriginID, new Vector4(origin.x, origin.y, origin.z, 0f));
                material.SetVector(DiscForwardID, new Vector4(forward.x, forward.y, forward.z, 0f));
                material.SetFloat(CurrentRadiusID, currentRadius);

                yield return null;
            }

            Destroy(material);
            Destroy(sphere);
            Destroy(gameObject);
        }
    }
}
