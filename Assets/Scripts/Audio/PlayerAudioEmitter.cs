using PurrNet;
using Resonance.Audio;
using UnityEngine;

namespace Resonance.PlayerController
{
    // Centralized networked audio emitter. Plain MonoBehaviours call EmitSound() on the
    // local instance and it handles ObserversRpc broadcast + audio reactive registration.
    public class PlayerAudioEmitter : NetworkBehaviour
    {
        public static PlayerAudioEmitter Local { get; private set; }

        protected override void OnSpawned()
        {
            base.OnSpawned();
            if (isOwner)
            {
                Local = this;
                enabled = true;
            }
            else
            {
                enabled = false;
            }
        }

        protected override void OnDespawned(bool asServer)
        {
            base.OnDespawned(asServer);
            if (isOwner)
                Local = null;
        }

        public void EmitSound(string wwiseEvent, Vector3 position, float duration = 1f)
        {
            BroadcastSound(wwiseEvent, position, duration);
        }

        public void RegisterSound(Vector3 position, float duration = 1f)
        {
            BroadcastRegistration(position, duration);
        }

        [ObserversRpc(runLocally: true)]
        private void BroadcastSound(string wwiseEvent, Vector3 position, float duration)
        {
#if !UNITY_SERVER
            if (!string.IsNullOrEmpty(wwiseEvent))
                AkUnitySoundEngine.PostEvent(wwiseEvent, gameObject);

            if (AudioSourceTracker.Instance != null)
                AudioSourceTracker.Instance.RegisterSound(position, duration);
#endif
        }

        [ObserversRpc(runLocally: true)]
        private void BroadcastRegistration(Vector3 position, float duration)
        {
#if !UNITY_SERVER
            if (AudioSourceTracker.Instance != null)
                AudioSourceTracker.Instance.RegisterSound(position, duration);
#endif
        }
    }
}
