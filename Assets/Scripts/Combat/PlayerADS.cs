using Unity.Cinemachine;
using PurrNet;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.PlayerController
{
    public class PlayerADS : NetworkBehaviour
    {
        [Header("FOV Settings")]
        public float normalFOV = 80f;
        public float zoomedFOV = 30f;

        [Header("Zoom Speed")]
        public float zoomSpeed = 10f;

        private CinemachineCamera _virtualCamera;
        private PlayerActionsInput _playerActionsInput;

        protected override void OnSpawned()
        {
            base.OnSpawned();
            enabled = isOwner;
        }

        private void Awake()
        {
            _playerActionsInput = PlayerActionsInput.Instance;
            _virtualCamera = GetComponentInChildren<CinemachineCamera>();
            _virtualCamera.Lens.FieldOfView = normalFOV;
        }

        private void Update()
        {
            float targetFOV = _playerActionsInput.AdsHeld ? zoomedFOV : normalFOV;

            _virtualCamera.Lens.FieldOfView = Mathf.Lerp(
                _virtualCamera.Lens.FieldOfView,
                targetFOV,
                Time.deltaTime * zoomSpeed
            );
        }
    }
}
