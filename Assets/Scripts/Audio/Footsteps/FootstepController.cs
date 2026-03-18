using UnityEngine;
using Resonance.PlayerController;

namespace Resonance.Audio
{
    public class FootstepController : MonoBehaviour
    {
        [Header("Wwise Events")] 
        public AK.Wwise.Event footstepEvent;
        
        [Header("Landing event (uses SurfaceType switch")]
        public AK.Wwise.Event landingEvent;

        [Header("Surface Detection")] 
        [Tooltip("Raycast distance for ground detection")]
        public float raycastDistance = 0.5f;

        [Tooltip("Layer mask for ground")]
        public LayerMask groundLayers;

        [Header("Surface Switches")] 
        public AK.Wwise.Switch concreteSurface;
        public AK.Wwise.Switch metalSurface;
        public AK.Wwise.Switch woodSurface;
        public AK.Wwise.Switch gravelSurface;
        
        private CharacterController characterController;
        private PlayerState playerState;
        private string currentSurface = "Concrete"; // concrete is default
        private bool wasInAir = false;

        void Awake()
        {
            characterController = GetComponentInParent<CharacterController>();
            playerState = GetComponentInParent<PlayerState>();

            if (characterController == null)
            {
                Debug.LogError("[FootstepController] CharacterController not found in parent!");
            }
            
            if (playerState == null)
            {
                Debug.LogError("[FootstepController] PlayerState not found in parent!");
            }
        }

        void Update()
        {
            // auto-detect landing
            bool isInAir = !playerState.InGroundedState();

            if (wasInAir && !isInAir)
            {
                PlayLanding();
            }

            wasInAir = isInAir;
        }

        public void PlayFootstep()
        {
            DetectSurface();
            SetSurfaceSwitch();

            if (footstepEvent != null && footstepEvent.IsValid())
            {
                footstepEvent.Post(gameObject);

                if (AudioSourceTracker.Instance != null)
                {
                    AudioSourceTracker.Instance.RegisterSound(transform.position, 0.3f);
                }
            }
        }

        public void PlayLanding()
        {
            DetectSurface();
            SetSurfaceSwitch();

            if (landingEvent != null && landingEvent.IsValid())
            {
                landingEvent.Post(gameObject);
                
                // Wait a tiny bit for Wwise Meter to update, then register
                if (AudioSourceTracker.Instance != null)
                {
                    Invoke(nameof(RegisterLanding), 0.05f); // 50ms delay
                }
            }
        }

        private void RegisterLanding()
        {
            if (AudioSourceTracker.Instance != null)
            {
                AudioSourceTracker.Instance.RegisterSound(transform.position, 0.5f);
            }
        }

        void DetectSurface()
        {
            Vector3 origin = transform.position + characterController.center;
            float distance = (characterController.height / 2f) + raycastDistance;
            
            RaycastHit hit;
            if (Physics.Raycast(origin, Vector3.down, out hit, distance, groundLayers))
            {
                currentSurface = GetSurfaceFromTag(hit.collider.tag);
            }
        }

        string GetSurfaceFromTag(string tag)
        {
            if (tag == "Concrete") return "Concrete";
            if (tag == "Metal" || tag == "Train") return "Metal";
            if (tag == "Wood") return "Wood";
            if (tag == "Gravel") return "Gravel";
            
            // default to concrete if tag not recognized
            return "Concrete";
        }

        void SetSurfaceSwitch()
        {
            switch (currentSurface)
            {
                case "Concrete":
                    concreteSurface?.SetValue(gameObject);
                    break;
                case "Metal":
                    metalSurface?.SetValue(gameObject);
                    break;
                case "Wood":
                    woodSurface?.SetValue(gameObject);
                    break;
                case "Gravel":
                    gravelSurface?.SetValue(gameObject);
                    break;
            }
        }

        void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || characterController == null) return;
            
            Vector3 origin = transform.position + characterController.center;
            float distance = (characterController.height / 2f) + raycastDistance;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, origin + Vector3.down * distance);
        }
    }
}