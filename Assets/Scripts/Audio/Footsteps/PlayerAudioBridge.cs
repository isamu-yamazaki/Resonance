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

        public void PlayFootstep()
        {
            if (footstepController == null) return;

            footstepController.PlayFootstep();

#if !UNITY_SERVER
            if (PlayerAudioEmitter.Local != null)
                PlayerAudioEmitter.Local.EmitSound("Play_Footstep", transform.position, 0.3f);
#endif
        }
    }
}
