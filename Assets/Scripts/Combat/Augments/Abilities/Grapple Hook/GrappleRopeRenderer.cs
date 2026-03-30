using PurrNet;
using Resonance.Player;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Combat.Augments
{
    public class GrappleRopeRenderer : NetworkBehaviour
    {
        [SerializeField] private GameObject ropeRendererPrefab;

        private LineRenderer lineRenderer;
        private Transform ropeOrigin;
        private PlayerSkinRenderer playerSkinRenderer;

        private SyncVar<Vector3> hookPoint = new SyncVar<Vector3>(default, 0f, ownerAuth: true);
        private SyncVar<bool> isGrappling = new SyncVar<bool>(default, 0f, ownerAuth: true);

        public SyncVar<Vector3> HookPoint => hookPoint;
        public SyncVar<bool> IsGrappling => isGrappling;

        private void Awake()
        {
            playerSkinRenderer = GetComponent<PlayerSkinRenderer>();
            playerSkinRenderer.OnNewSkinSpawned += OnSkinSpawned;

            if (ropeRendererPrefab != null)
            {
                GameObject ropeInstance = Instantiate(ropeRendererPrefab, transform);
                lineRenderer = ropeInstance.GetComponent<LineRenderer>();
                lineRenderer.positionCount = 2;
                lineRenderer.enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (playerSkinRenderer != null)
            {
                playerSkinRenderer.OnNewSkinSpawned -= OnSkinSpawned;
            }
        }

        private void OnSkinSpawned(GameObject skinInstance)
        {
            Transform[] children = skinInstance.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                if (child.CompareTag("GrappleOrigin"))
                {
                    ropeOrigin = child;
                    break;
                }
            }
        }

        private void Update()
        {
            if (lineRenderer == null)
            {
                return;
            }

            lineRenderer.enabled = isGrappling.value;

            if (isGrappling.value)
            {
                Vector3 origin = ropeOrigin != null ? ropeOrigin.position : transform.position;
                lineRenderer.SetPosition(0, origin);
                lineRenderer.SetPosition(1, hookPoint.value);
            }
        }
    }
}