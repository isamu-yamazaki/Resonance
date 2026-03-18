using UnityEngine;
using Resonance.Audio;

namespace Resonance.PlayerController
{
    // Forwards animation events to audio components
    // Required because animation events can only call methods on the GameObject with the Animator
    public class PlayerAudioBridge : MonoBehaviour
    {
        [Header("Audio Components")]
        [SerializeField] private FootstepController  footstepController;

        public void PlayFootstep()
        {
            if (footstepController != null)
            {
                footstepController.PlayFootstep();
            }
        }
    }
}
