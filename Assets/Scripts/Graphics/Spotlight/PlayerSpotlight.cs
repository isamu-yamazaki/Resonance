using System.Collections;
using PurrNet;
using Resonance.Match;
using UnityEngine;

namespace Resonance
{
    public class PlayerSpotlight : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Material volumetricMaterial;

        [Header("Tracking")]
        [SerializeField] private float heightAbovePlayer = 8f;
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private float lightRange = 50f;

        [Header("Transition")]
        [SerializeField] private float switchOffDuration = 0.5f;

        [Header("Audio")]
#if !UNITY_SERVER
        [SerializeField] private AK.Wwise.Event spotlightOnEvent;
#endif

        private Light _light;
        private GameObject _lightObj;
        private VolumetricLight volumetricLight;

        private Coroutine _switchCoroutine;

        protected override void OnSpawned(bool asServer)
        {
            base.OnSpawned(asServer);

            if (!asServer)
            {
                SpawnLightObj();
                SetActiveImmediate(false);
            }

            if (MatchLogicNetworkAdapter.Instance != null)
            {
                MatchLogicNetworkAdapter.Instance.OnFinishedConfiguring += OnMatchLogicConfigured;

                if (MatchLogicNetworkAdapter.Instance.HasFinishedConfiguring)
                    OnMatchLogicConfigured();
            }
        }

        private void OnMatchLogicConfigured()
        {
            var arenaRoundManager = ArenaRoundManagerBridge.GetTemporaryReference();
            if (arenaRoundManager != null)
                arenaRoundManager.OnLeaderChanged += OnLeaderChanged;
        }

        protected override void OnDespawned(bool asServer)
        {
            base.OnDespawned(asServer);

            if (_lightObj != null)
                Destroy(_lightObj);

            if (MatchLogicNetworkAdapter.Instance != null)
                MatchLogicNetworkAdapter.Instance.OnFinishedConfiguring -= OnMatchLogicConfigured;

            var arenaRoundManager = ArenaRoundManagerBridge.GetTemporaryReference();
            if (arenaRoundManager != null)
                arenaRoundManager.OnLeaderChanged -= OnLeaderChanged;
        }

        private void SpawnLightObj()
        {
            _lightObj = new GameObject("PlayerSpotlight_Light");

            _light = _lightObj.AddComponent<Light>();
            _light.type = LightType.Spot;
            _light.spotAngle = 30f;
            _light.range = lightRange;
            _light.intensity = 15f;
            _light.shadows = LightShadows.None;

            _lightObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            _lightObj.SetActive(false);
            volumetricLight = _lightObj.AddComponent<VolumetricLight>();
            volumetricLight.Material = volumetricMaterial;
            _lightObj.SetActive(true);
        }

        private void OnLeaderChanged(PlayerID id, float rating)
        {
            bool isLeader = owner == id;
#if UNITY_EDITOR
            if (isLeader)
                Debug.Log($"[PlayerSpotlight] {id} has the spotlight");
#endif

            if (_switchCoroutine != null)
                StopCoroutine(_switchCoroutine);

            if (isLeader)
                _switchCoroutine = StartCoroutine(SwitchOn());
            else
                SetActiveImmediate(false);
        }

        private IEnumerator SwitchOn()
        {
            SetActiveImmediate(false);
            yield return new WaitForSeconds(switchOffDuration);

            SetActiveImmediate(true);
            PostSpotlightOn();
        }

        private void PostSpotlightOn()
        {
#if !UNITY_SERVER
            spotlightOnEvent?.Post(gameObject);
#endif
        }

        private void SetActiveImmediate(bool active)
        {
            if (volumetricLight != null)
                volumetricLight.enabled = active;
            if (_light != null)
                _light.enabled = active;
        }

        private void LateUpdate()
        {
            if (_lightObj == null) return;

            Vector3 lightPos = new Vector3(
                transform.position.x,
                transform.position.y + heightAbovePlayer,
                transform.position.z
            );
            _lightObj.transform.position = Vector3.Lerp(_lightObj.transform.position, lightPos, Time.deltaTime * smoothSpeed);
            _lightObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}
