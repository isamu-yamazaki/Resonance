using PurrNet;
using UnityEngine;

namespace Resonance
{
    public class PlayerSpotlight : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Material spotlightMaterial;

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
            beamObject.transform.SetParent(transform);
            beamObject.transform.localPosition = new Vector3(0f, beamHeight * 0.5f, 0f);
            beamObject.transform.localRotation = Quaternion.identity;
            beamObject.transform.localScale = new Vector3(beamWidth, beamHeight * 0.5f, beamWidth);

            Destroy(beamObject.GetComponent<CapsuleCollider>());

            beamMaterialInstance = new Material(spotlightMaterial);
            beamMaterialInstance.SetColor("_Color", beamColor);
            beamMaterialInstance.SetFloat("_Intensity", beamIntensity);
            beamObject.GetComponent<MeshRenderer>().material = beamMaterialInstance;
        }

        private void SpawnLight()
        {
            lightObject = new GameObject("PlayerSpotlight_Light");
            lightObject.transform.SetParent(transform);
            lightObject.transform.localPosition = new Vector3(0f, beamHeight, 0f);
            lightObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            Light spotlight = lightObject.AddComponent<Light>();
            spotlight.type = LightType.Spot;
            spotlight.color = beamColor;
            spotlight.intensity = lightIntensity;
            spotlight.range = lightRange;
            spotlight.spotAngle = lightSpotAngle;
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
