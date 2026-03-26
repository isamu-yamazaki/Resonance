using UnityEngine;
using Resonance.Audio;

namespace Resonance.PlayerController
{
    // Forwards animation events to audio components
    // Required because animation events can only call methods on the GameObject with the Animator
    public class PlayerAudioBridge : MonoBehaviour
    {
#if !UNITY_SERVER
        [Header("Audio Components")]
        [SerializeField] private FootstepController  footstepController;
#endif

        public void PlayFootstep()
        {
#if !UNITY_SERVER
            if (footstepController != null)
            {
                footstepController.PlayFootstep();
            }
#endif
        }
    }
}
