using PurrNet;
using UnityEngine;
using UnityEngine.VFX;

namespace Resonance
{
    public class PlayerSpotlight : NetworkBehaviour
    {
        [Header("VFX")]
        [SerializeField] private VisualEffect spotlightEffectPrefab;
        [SerializeField] private Vector3 localOffset = new Vector3(0f, 2.5f, 0f);

        [Header("Appearance")]
        [SerializeField] private Color spotlightColor = new Color(0.6f, 0.85f, 1f, 1f);
        [SerializeField] private float spotlightAlpha = 0.35f;

        private VisualEffect spotlightInstance;

        // VFX Graph exposed property names
        private static readonly int colorPropertyID = Shader.PropertyToID("SpotlightColor");
        private static readonly int alphaPropertyID = Shader.PropertyToID("SpotlightAlpha");

        protected override void OnSpawned(bool asServer)
        {
            base.OnSpawned(asServer);

            if (!asServer)
                SpawnSpotlight();
        }

        protected override void OnDespawned(bool asServer)
        {
            base.OnDespawned(asServer);

            if (spotlightInstance != null)
                Destroy(spotlightInstance.gameObject);
        }

        private void SpawnSpotlight()
        {
            if (spotlightEffectPrefab == null)
            {
                Debug.LogError("[PlayerSpotlight] No VFX effect prefab assigned!");
                return;
            }

            spotlightInstance = Instantiate(spotlightEffectPrefab, transform);
            spotlightInstance.transform.localPosition = localOffset;
            spotlightInstance.transform.localRotation = Quaternion.identity;
            spotlightInstance.gameObject.name = "PlayerSpotlight_VFX";

            ApplyVisualProperties();
            spotlightInstance.Play();
        }

        private void ApplyVisualProperties()
        {
            if (spotlightInstance == null) return;

            if (spotlightInstance.HasVector4(colorPropertyID))
                spotlightInstance.SetVector4(colorPropertyID, spotlightColor);

            if (spotlightInstance.HasFloat(alphaPropertyID))
                spotlightInstance.SetFloat(alphaPropertyID, spotlightAlpha);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Live-update in editor when tweaking color/alpha
            if (spotlightInstance != null)
                ApplyVisualProperties();
        }
#endif
    }
}
