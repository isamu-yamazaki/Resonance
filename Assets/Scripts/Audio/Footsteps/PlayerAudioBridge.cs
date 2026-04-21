using UnityEngine;
using Resonance.Audio;

namespace Resonance.PlayerController
{
    // Forwards animation events to audio components
    // Required because animation events can only call methods on the GameObject with the Animator
    public class PlayerAudioBridge : MonoBehaviour
    {
        [Header("Audio Components")]
        [SerializeField] private FootstepController footstepController;

        [Header("Tuning")]
        [SerializeField] private float footstepCooldown = 0.2f;

        private float lastFootstepTime;

        public void PlayFootstep()
        {
            if (Time.time - lastFootstepTime < footstepCooldown) return;
            lastFootstepTime = Time.time;

            if (footstepController != null)
            {
                footstepController.PlayFootstep();

#if !UNITY_SERVER
                if (AudioSourceTracker.Instance != null)
                    AudioSourceTracker.Instance.RegisterSound(transform.position, 0.3f);
#endif
            }
        }
    }
}
