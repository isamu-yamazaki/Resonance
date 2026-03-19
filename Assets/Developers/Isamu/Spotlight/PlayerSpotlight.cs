using PurrNet;
using UnityEngine;

namespace Resonance
{
    public class PlayerSpotlight : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Material spotlightMaterial;
        [SerializeField] private LayerMask groundLayer;

        [Header("Appearance")]
        [SerializeField] private Color beamColor = new Color(0.9f, 0.95f, 1f, 1f);

        [Header("Beam (Visual)")]
        [SerializeField] private float beamIntensity = 600f;
        [SerializeField] private float beamWidth = 3f;
        [SerializeField] private float beamHeight = 20f;

        [Header("Light (Ground)")]
        [SerializeField] private float lightIntensity = 5f;
        [SerializeField] private float lightRange = 25f;
        [SerializeField] private float lightSpotAngle = 30f;

        private GameObject beamObject;
        private GameObject lightObject;
        private Material beamMaterialInstance;
        private MeshRenderer beamRenderer;

        protected override void OnSpawned(bool asServer)
        {
            base.OnSpawned(asServer);

            if (!asServer)
            {
                SpawnBeam();
                SpawnLight();
            }
        }

        protected override void OnDespawned(bool asServer)
        {
            base.OnDespawned(asServer);

            if (beamMaterialInstance != null)
                Destroy(beamMaterialInstance);
            if (beamObject != null)
                Destroy(beamObject);
            if (lightObject != null)
                Destroy(lightObject);
        }

        private void SpawnBeam()
        {
            if (spotlightMaterial == null)
            {
                Debug.LogError("[PlayerSpotlight] No spotlight material assigned!");
                return;
            }

            beamObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beamObject.name = "PlayerSpotlight_Beam";

            // Detach from player — we'll manually update position in Update
            beamObject.transform.SetParent(null);

            Destroy(beamObject.GetComponent<CapsuleCollider>());

            beamMaterialInstance = new Material(spotlightMaterial);
            beamMaterialInstance.SetColor("_Color", beamColor);
            beamMaterialInstance.SetFloat("_Intensity", beamIntensity);

            beamRenderer = beamObject.GetComponent<MeshRenderer>();
            beamRenderer.material = beamMaterialInstance;
        }

        private void SpawnLight()
        {
            lightObject = new GameObject("PlayerSpotlight_Light");

            // Also detach light — manually tracked
            lightObject.transform.SetParent(null);
            lightObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            Light spotlight = lightObject.AddComponent<Light>();
            spotlight.type = LightType.Spot;
            spotlight.color = beamColor;
            spotlight.intensity = lightIntensity;
            spotlight.range = lightRange;
            spotlight.spotAngle = lightSpotAngle;
        }

        private void LateUpdate()
        {
            if (beamObject == null && lightObject == null) return;

            // Raycast down from high above the player to find the ground
            Vector3 playerXZ = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 rayOrigin = new Vector3(transform.position.x, transform.position.y + beamHeight, transform.position.z);

            float groundY = 0f;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, beamHeight * 2f, groundLayer))
            {
                groundY = hit.point.y;
            }
            else
            {
                // Fallback — assume flat ground at y=0
                groundY = 0f;
            }

            float topY = groundY + beamHeight;
            float actualHeight = topY - groundY; // = beamHeight, but correct if terrain varies
            float centerY = groundY + actualHeight * 0.5f;

            // Position beam centered between ground and top
            if (beamObject != null)
            {
                beamObject.transform.position = new Vector3(transform.position.x, centerY, transform.position.z);
                beamObject.transform.rotation = Quaternion.identity;
                beamObject.transform.localScale = new Vector3(beamWidth, actualHeight * 0.5f, beamWidth);
            }

            // Position light at the top of the beam pointing down
            if (lightObject != null)
            {
                lightObject.transform.position = new Vector3(transform.position.x, topY, transform.position.z);
                lightObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (beamMaterialInstance != null)
            {
                beamMaterialInstance.SetColor("_Color", beamColor);
                beamMaterialInstance.SetFloat("_Intensity", beamIntensity);
            }

            if (lightObject != null)
            {
                Light spotlight = lightObject.GetComponent<Light>();
                if (spotlight != null)
                {
                    spotlight.color = beamColor;
                    spotlight.intensity = lightIntensity;
                    spotlight.range = lightRange;
                    spotlight.spotAngle = lightSpotAngle;
                }
            }
        }
#endif
    }
}
