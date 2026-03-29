#if !UNITY_SERVER
using UnityEngine;

namespace Resonance.Combat.Weapons
{
    public class WeaponView : MonoBehaviour
    {
        [Header("Muzzle")]
        [SerializeField] private Transform muzzle;
        public Transform Muzzle => muzzle;

        [Header("Muzzle Flash")]
        [SerializeField] private MuzzleFlash muzzleFlash;

        [Header("Audio Emitters")]
        [SerializeField] private GameObject muzzleEmitter;
        [SerializeField] private GameObject bodyEmitter;

        [Header("Casing Settings")]
        [SerializeField] private float casingGroundThreshold = 3f;
        [SerializeField] private LayerMask casingLayerMask;

        private WeaponAudioProperties audioProperties;

        private void Awake()
        {
            SetupEmitter(muzzleEmitter);
            SetupEmitter(bodyEmitter);

            if (casingLayerMask == 0)
                casingLayerMask = 1 << LayerMask.NameToLayer("Environment");
        }

        // ─── Emitter Setup ────────────────────────────────────────────────────────

        private void SetupEmitter(GameObject emitter)
        {
            if (emitter == null) return;

            if (emitter.GetComponent<AkGameObj>() == null)
                emitter.AddComponent<AkGameObj>();

            if (emitter.GetComponent<WwiseSmartOcclusion>() == null)
                emitter.AddComponent<WwiseSmartOcclusion>();
        }

        // ─── Muzzle Flash ─────────────────────────────────────────────────────────

        public void PlayMuzzleFlash()
        {
            if (muzzleFlash != null)
                muzzleFlash.Play();
        }

        public void ApplyMuzzleFlashSettings(MuzzleFlashSettings settings)
        {
            if (muzzleFlash != null && settings != null)
                muzzleFlash.ApplySettings(settings);
        }

        // ─── Audio ────────────────────────────────────────────────────────────────

        public void ApplyAudioProperties(WeaponAudioProperties properties)
        {
            audioProperties = properties;
        }

        public void PlayFire()
        {
            if (audioProperties?.fireEvent == null) return;
            if (muzzleEmitter == null) return;
            audioProperties.fireEvent.Post(muzzleEmitter);

            TryPlayCasing();
        }

        public void PlayEmptyTrigger()
        {
            if (audioProperties?.emptyTriggerEvent == null) return;
            if (muzzleEmitter == null) return;
            audioProperties.emptyTriggerEvent.Post(muzzleEmitter);
        }

        public void PlayEquip()
        {
            if (audioProperties?.equipEvent == null) return;
            if (bodyEmitter == null) return;
            audioProperties.equipEvent.Post(bodyEmitter);
        }

        public void PlayReload()
        {
            if (audioProperties?.reloadEvent == null) return;
            if (bodyEmitter == null) return;
            audioProperties.reloadEvent.Post(bodyEmitter);
        }

        private void TryPlayCasing()
        {
            if (audioProperties?.casingEvent == null) return;
            if (bodyEmitter == null) return;

            Vector3 rootPosition = transform.root.position;
            if (Physics.Raycast(rootPosition, Vector3.down, out RaycastHit hit, casingGroundThreshold, casingLayerMask))
                audioProperties.casingEvent.Post(bodyEmitter);
        }
    }
}
#else
using UnityEngine;

namespace Resonance.Combat.Weapons
{
    public class WeaponView : MonoBehaviour
    {
        [SerializeField] private Transform muzzle;
        public Transform Muzzle => muzzle;

        public void PlayMuzzleFlash() { }
        public void ApplyMuzzleFlashSettings(MuzzleFlashSettings settings) { }
        public void ApplyAudioProperties(WeaponAudioProperties properties) { }
        public void PlayFire() { }
        public void PlayEmptyTrigger() { }
        public void PlayEquip() { }
        public void PlayReload() { }
    }
}
#endif
