using PurrNet;
using Resonance.Match;
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
        [SerializeField] private float lightSmoothSpeed = 5f;

        private GameObject beamObject;
        private GameObject lightObject;
        private Material beamMaterialInstance;
        private MeshRenderer beamRenderer;
        private Light spotLight;
        private float lastKnownGroundY = 0f;

        protected override void OnSpawned(bool asServer)
        {
            base.OnSpawned(asServer);

            if (!asServer)
            {
                SpawnBeam();
                SpawnLight();
                beamObject.SetActive(false);
                lightObject.SetActive(false);
            }

            if (ArenaRoundManagerBridge.Instance != null)
            {
                ArenaRoundManagerBridge.Instance.OnLeaderChanged += OnLeaderChanged;
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

            if (ArenaRoundManagerBridge.Instance != null)
            {
                ArenaRoundManagerBridge.Instance.OnLeaderChanged -= OnLeaderChanged;
            }
        }

        private void OnLeaderChanged(PlayerID id, float rating)
        {
            bool isLeader = owner == id;
            if (isLeader)
                Debug.Log($"[PlayerSpotlight] {id} has the spotlight");

            if (beamObject != null)
                beamObject.SetActive(isLeader);
            if (lightObject != null)
                lightObject.SetActive(isLeader);
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
            lightObject.transform.SetParent(null);
            lightObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            spotLight = lightObject.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.color = beamColor;
            spotLight.intensity = lightIntensity;
            spotLight.range = lightRange;
            spotLight.spotAngle = lightSpotAngle;
        }

        private void LateUpdate()
        {
            if (beamObject == null && lightObject == null) return;

            // Raycast downward from player feet — ignore anything above
            Vector3 rayOrigin = transform.position;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, beamHeight * 2f, groundLayer))
            {
                if (hit.point.y < transform.position.y)
                    lastKnownGroundY = hit.point.y;
            }

            float groundY = lastKnownGroundY;
            float topY = groundY + beamHeight;
            float actualHeight = topY - groundY;
            float centerY = groundY + actualHeight * 0.5f;

            if (beamObject != null)
            {
                Vector3 targetBeamPos = new Vector3(transform.position.x, centerY, transform.position.z);
                beamObject.transform.position = Vector3.Lerp(beamObject.transform.position, targetBeamPos, Time.deltaTime * lightSmoothSpeed);
                beamObject.transform.rotation = Quaternion.identity;
                beamObject.transform.localScale = new Vector3(beamWidth, actualHeight * 0.5f, beamWidth);
            }

            if (lightObject != null && spotLight != null)
            {
                Vector3 targetLightPos = new Vector3(transform.position.x, topY, transform.position.z);
                lightObject.transform.position = Vector3.Lerp(lightObject.transform.position, targetLightPos, Time.deltaTime * lightSmoothSpeed);
                lightObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                float playerHeightAboveGround = transform.position.y - groundY;
                float heightRatio = Mathf.Clamp01(playerHeightAboveGround / beamHeight);
                float targetIntensity = Mathf.Lerp(lightIntensity, 0f, heightRatio);
                spotLight.intensity = Mathf.Lerp(spotLight.intensity, targetIntensity, Time.deltaTime * lightSmoothSpeed);
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

            if (spotLight != null)
            {
                spotLight.color = beamColor;
                spotLight.intensity = lightIntensity;
                spotLight.range = lightRange;
                spotLight.spotAngle = lightSpotAngle;
            }
        }
#endif
    }
}
