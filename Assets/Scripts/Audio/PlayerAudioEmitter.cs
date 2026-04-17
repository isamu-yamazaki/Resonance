using PurrNet;
using Resonance.Audio;
using UnityEngine;

namespace Resonance.PlayerController
{
    // Centralized networked audio emitter. Plain MonoBehaviours call EmitSound() on the local instance and it handles ServerRpc -> ObserversRpc broadcast + audio reactive registration.
    public class PlayerAudioEmitter : NetworkBehaviour
    {
        public static PlayerAudioEmitter Local { get; private set; }

        protected override void OnSpawned()
        {
            base.OnSpawned();
            if (isOwner)
                Local = this;
        }

        protected override void OnDespawned(bool asServer)
        {
            base.OnDespawned(asServer);
            if (isOwner)
                Local = null;
        }

        // Call from any owner-side MonoBehaviour to broadcast a sound to all clients
        public void EmitSound(string wwiseEvent, Vector3 position, float duration = 1f)
        {
            RequestSoundOnServer(wwiseEvent, position, duration);
        }

        [ServerRpc]
        private void RequestSoundOnServer(string wwiseEvent, Vector3 position, float duration)
        {
            BroadcastSound(wwiseEvent, position, duration);
        }

        [ObserversRpc(runLocally: true)]
        private void BroadcastSound(string wwiseEvent, Vector3 position, float duration)
        {
#if !UNITY_SERVER
            AkUnitySoundEngine.PostEvent(wwiseEvent, gameObject);

            if (AudioSourceTracker.Instance != null)
                AudioSourceTracker.Instance.RegisterSound(position, duration);
#endif
        }
    }
}
