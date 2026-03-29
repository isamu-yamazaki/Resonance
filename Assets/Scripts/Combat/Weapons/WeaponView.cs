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

        private WeaponAudioProperties audioProperties;

        private void Awake()
        {
            SetupEmitter(muzzleEmitter);
            SetupEmitter(bodyEmitter);
        }

        private void SetupEmitter(GameObject emitter)
        {
            if (emitter == null) return;

            if (emitter.GetComponent<AkGameObj>() == null)
                emitter.AddComponent<AkGameObj>();

            if (emitter.GetComponent<WwiseSmartOcclusion>() == null)
                emitter.AddComponent<WwiseSmartOcclusion>();
        }

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

        public void ApplyAudioProperties(WeaponAudioProperties properties)
        {
            audioProperties = properties;
        }

        public void PlayFire()
        {
            if (audioProperties?.fireEvent == null) return;
            if (muzzleEmitter == null) return;
            audioProperties.fireEvent.Post(muzzleEmitter);
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
    }
}
#else
using UnityEngine;

namespace Resonance.Combat.Weapons
{
    public class WeaponView : MonoBehaviour
    {
        [Header("Muzzle")]
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
