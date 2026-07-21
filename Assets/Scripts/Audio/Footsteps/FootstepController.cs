using UnityEngine;
using Resonance.Assemblies.Player;
using Resonance.PlayerController;
using PurrNet;

namespace Resonance.Audio
{
    public class FootstepController : NetworkBehaviour
    {
#if !UNITY_SERVER
        [Header("Wwise Events")]
        public AK.Wwise.Event footstepEvent;

        [Header("Landing event (uses SurfaceType switch)")]
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
        public AK.Wwise.Switch grassSurface;

        [Header("Movement Switches")]
        public AK.Wwise.Switch runSwitch;
        public AK.Wwise.Switch sprintSwitch;
#endif

        private CharacterController characterController;
        private PlayerState playerState;
        private string currentSurface = "Concrete";
        private bool wasInAir = false;

        protected override void OnSpawned()
        {
            base.OnSpawned();
        }

        void Awake()
        {
            characterController = GetComponentInParent<CharacterController>();
            playerState = GetComponentInParent<PlayerState>();

            if (characterController == null)
                Debug.LogError("[FootstepController] CharacterController not found in parent!");

            if (playerState == null)
                Debug.LogError("[FootstepController] PlayerState not found in parent!");
        }

        void Update()
        {
            if (!isOwner) return;

            bool isInAir = !playerState.InGroundedState();
            bool canLand = playerState.InGroundedState() ||
                           playerState.CurrentPlayerMovementState == PlayerMovementState.Jumping ||
                           playerState.CurrentPlayerMovementState == PlayerMovementState.Falling;

            if (wasInAir && !isInAir && canLand)
                PlayLanding();

            wasInAir = isInAir && canLand;
        }

        public void PlayFootstep()
        {
            if (!isOwner) return;
            PlayFootstepRpc();
        }

        public void PlayLanding()
        {
            if (!isOwner) return;
            PlayLandingRpc();
        }

        [ObserversRpc(runLocally: true)]
        private void PlayFootstepRpc()
        {
#if !UNITY_SERVER
            DetectSurface();
            SetSurfaceSwitch();
            SetMovementSwitch();

            if (footstepEvent != null && footstepEvent.IsValid())
                footstepEvent.Post(gameObject);
#endif
        }

        [ObserversRpc(runLocally: true)]
        private void PlayLandingRpc()
        {
#if !UNITY_SERVER
            DetectSurface();
            SetSurfaceSwitch();

            if (landingEvent != null && landingEvent.IsValid())
            {
                landingEvent.Post(gameObject);

                if (AudioSourceTracker.Instance != null)
                    AudioSourceTracker.Instance.RegisterSound(transform.position, 0.5f);
            }
#endif
        }

#if !UNITY_SERVER
        void DetectSurface()
        {
            Vector3 origin = transform.position + characterController.center;
            float distance = (characterController.height / 2f) + raycastDistance;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, groundLayers))
                currentSurface = GetSurfaceFromTag(hit.collider.tag);
        }

        string GetSurfaceFromTag(string tag)
        {
            if (tag == "Concrete") return "Concrete";
            if (tag == "Metal" || tag == "Train") return "Metal";
            if (tag == "Wood") return "Wood";
            if (tag == "Gravel") return "Gravel";
            if (tag == "Grass") return "Grass";
            return "Concrete";
        }

        void SetSurfaceSwitch()
        {
            switch (currentSurface)
            {
                case "Concrete": concreteSurface?.SetValue(gameObject); break;
                case "Metal": metalSurface?.SetValue(gameObject); break;
                case "Wood": woodSurface?.SetValue(gameObject); break;
                case "Gravel": gravelSurface?.SetValue(gameObject); break;
                case "Grass": grassSurface?.SetValue(gameObject); break;
            }
        }

        void SetMovementSwitch()
        {
            bool isSprinting = playerState != null &&
                               playerState.CurrentPlayerMovementState == PlayerMovementState.Sprinting;

            if (isSprinting)
                sprintSwitch?.SetValue(gameObject);
            else
                runSwitch?.SetValue(gameObject);
        }

        void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || characterController == null) return;

            Vector3 origin = transform.position + characterController.center;
            float distance = (characterController.height / 2f) + raycastDistance;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, origin + Vector3.down * distance);
        }
#endif
    }
}
