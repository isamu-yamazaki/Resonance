using System.Collections.Generic;
using PurrNet;
using UnityEngine;

namespace Resonance.Environment
{
    public class GlassShatterEffect : NetworkBehaviour
    {
        [Header("Pane Properties")]
        [Tooltip("Thickness of the glass pane (Z scale). Locked to match shard thickness.")]
        [SerializeField] [Range(0.05f, 0.1f)] private float shardThickness = 0.07f;

        [Tooltip("Shards spawned per square unit of pane area. 5 = 20 shards on a 2x2 pane.")]
        [SerializeField] private float shardDensity = 5f;

        [Tooltip("Material assigned to all generated shards. Must be URP Transparent.")]
        [SerializeField] private Material shardMaterial;

        [Tooltip("Mass of each shard Rigidbody. Higher = less explosive reaction to forces.")]
        [SerializeField] private float shardMass = 10f;

        [Header("Force Settings")]
        [SerializeField] private float baseExplosionForce = 4f;
        [SerializeField] private float directionalForceBias = 17f;
        [SerializeField] [Range(0f, 1f)] private float upwardBias = 0.1f;
        [SerializeField] private float explosionRadius = 0.3f;
        [SerializeField] private float torqueMin = 2f;
        [SerializeField] private float torqueMax = 4f;

        [Header("Lifetime Settings")]
        [SerializeField] private float fadeDelay = 4f;
        [SerializeField] private float fadeDuration = 3f;

        private void OnValidate()
        {
            Vector3 scale = transform.localScale;
            if (!Mathf.Approximately(scale.z, shardThickness))
            {
                scale.z = shardThickness;
                transform.localScale = scale;
            }
        }

        public void Shatter(Vector3 hitPoint, Vector3 hitNormal, Vector3 hitDirection)
        {
            if (shardMaterial == null)
            {
                Debug.LogWarning("[GlassShatterEffect] Shard Material is not assigned.", this);
                return;
            }

            Vector2 paneSize = new Vector2(transform.localScale.x, transform.localScale.y);
            float area       = paneSize.x * paneSize.y;
            int shardCount   = Mathf.Clamp(Mathf.RoundToInt(area * shardDensity), 8, 64);

            Vector3 hitLocal   = transform.InverseTransformPoint(hitPoint);
            Vector2 hitLocal2D = new Vector2(hitLocal.x, hitLocal.y);

            List<GameObject> shards = VoronoiGlassFracture.Generate(
                transform, paneSize, shardThickness, shardCount, hitLocal2D, shardMaterial, shardMass
            );

            Vector3 explosionOrigin = hitPoint - hitDirection.normalized * 0.05f;
            Vector3 biasDir = hitDirection.sqrMagnitude > 0.01f
                ? hitDirection.normalized
                : -hitNormal.normalized;

            foreach (GameObject shard in shards)
            {
                Rigidbody rb = shard.GetComponent<Rigidbody>();
                rb.AddExplosionForce(baseExplosionForce, explosionOrigin, explosionRadius, upwardBias, ForceMode.Impulse);
                rb.AddForce(biasDir * directionalForceBias, ForceMode.Impulse);
                rb.AddTorque(Random.onUnitSphere * Random.Range(torqueMin, torqueMax), ForceMode.Impulse);

                GlassShard glassShardComponent = shard.AddComponent<GlassShard>();
                glassShardComponent.Initialize(fadeDelay, fadeDuration);
            }

            // Register shatter with audio reactivity system after delay
            if (Audio.AudioSourceTracker.Instance != null)
            {
                StartCoroutine(RegisterShatterDelayed(hitPoint));
            }
        }

        private System.Collections.IEnumerator RegisterShatterDelayed(Vector3 position)
        {
            yield return new WaitForSeconds(0.05f);
            
            if (Audio.AudioSourceTracker.Instance != null)
            {
                float intensity = Audio.AudioBusMonitor.Instance != null 
                    ? Audio.AudioBusMonitor.Instance.GetMaxBusIntensity() 
                    : 0f;
                
                Debug.Log($"[GlassShatter] Registering at {position}, Bus Intensity: {intensity:F3}");
                Audio.AudioSourceTracker.Instance.RegisterSound(position, 1.5f);
            }
        }
    }
}
