using System.Collections.Generic;
using PurrNet;
using UnityEngine;

namespace Resonance.PlayerController
{
    public class OverdriveTrailEffect : NetworkBehaviour
    {
        #region Class Variables
        [Header("Trail Settings")]
        [SerializeField] private Material _overdriveTrailMaterial;
        [SerializeField] private float spawnInterval = 0.1f;
        [SerializeField] private float ghostLifetime = 0.5f;
        [SerializeField] private int maxGhosts = 10;
        [SerializeField] private bool enableGhostLinger = false;

        [Header("Color Settings")]
        [SerializeField] private Gradient colorGradient;
        [SerializeField] private bool useGradientOverLifetime = true;

        private SkinnedMeshRenderer[] _meshesToCopy;
        private PlayerSkinRenderer _playerSkinRenderer;
        private OverdriveAbility _overdriveAbility;
        private float _spawnTimer = 0f;
        private Queue<GhostInstance> _ghostPool = new Queue<GhostInstance>();
        private List<GhostInstance> _activeGhosts = new List<GhostInstance>();
        private Transform _ghostContainer;
        #endregion

        #region Startup
        private void Awake()
        {
            _overdriveAbility = GetComponent<OverdriveAbility>();

            // Create a static container for ghosts in world space
            GameObject container = new GameObject("Overdrive Ghost Container");
            _ghostContainer = container.transform;

            // Initialize default gradient if not set
            if (colorGradient == null || colorGradient.colorKeys.Length == 0)
            {
                colorGradient = new Gradient();
                GradientColorKey[] colorKeys = new GradientColorKey[3];
                colorKeys[0] = new GradientColorKey(new Color(0f, 1f, 0f), 0f);    // Green
                colorKeys[1] = new GradientColorKey(new Color(0f, 1f, 1f), 0.5f);  // Cyan
                colorKeys[2] = new GradientColorKey(new Color(0f, 0.5f, 1f), 1f);  // Blue

                GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
                alphaKeys[0] = new GradientAlphaKey(1f, 0f);
                alphaKeys[1] = new GradientAlphaKey(1f, 1f);

                colorGradient.SetKeys(colorKeys, alphaKeys);
            }

            _playerSkinRenderer = GetComponent<PlayerSkinRenderer>();
            _playerSkinRenderer.OnNewSkinSpawned += UpdateMeshesToRender;
        }

        private void Start()
        {
            // Pre-instantiate ghost pool
            for (int i = 0; i < maxGhosts; i++)
            {
                CreateGhostInstance();
            }
        }
        #endregion

        #region Update Logic
        private void Update()
        {
            if (_overdriveAbility == null || _meshesToCopy.Length == 0) return;

            if (_overdriveAbility.IsInOverdrive)
            {
                _spawnTimer += Time.deltaTime;

                if (_spawnTimer >= spawnInterval)
                {
                    SpawnGhost();
                    _spawnTimer = 0f;
                }
            }

            UpdateActiveGhosts();
        }

        private void UpdateActiveGhosts()
        {
            for (int i = _activeGhosts.Count - 1; i >= 0; i--)
            {
                GhostInstance ghost = _activeGhosts[i];
                ghost.lifetime += Time.deltaTime;

                float normalizedLifetime = ghost.lifetime / ghostLifetime;

                // Update alpha fade
                float alpha = 1f - normalizedLifetime;

                // Update color if using gradient
                Color targetColor = useGradientOverLifetime
                    ? colorGradient.Evaluate(normalizedLifetime)
                    : colorGradient.Evaluate(0f);
                targetColor.a = alpha;

                foreach (MeshRenderer meshRenderer in ghost.meshRenderers)
                {
                    foreach (Material material in meshRenderer.materials)
                    {
                        if (material != null)
                        {
                            material.color = targetColor;
                        }
                    }
                }

                // Return to pool when lifetime expires
                if (ghost.lifetime >= ghostLifetime)
                {
                    if (!enableGhostLinger)
                    {
                        ReturnGhostToPool(ghost);
                    }
                    _activeGhosts.RemoveAt(i);
                }
            }
        }
        #endregion

        #region Ghost Management
        public void UpdateMeshesToRender(GameObject root)
        {
            _meshesToCopy = root.GetComponentsInChildren<SkinnedMeshRenderer>();
            Debug.Log("[OverdriveTrailEffect] Updating trail effect based on new skin");
        }

        [ObserversRpc]
        private void SpawnGhost()
        {
            GhostInstance ghost = GetGhostFromPool();
            if (ghost == null) return;

            ghost.gameObject.SetActive(true);
            ghost.transform.position = transform.position;
            ghost.transform.rotation = transform.rotation;
            ghost.lifetime = 0f;

            // Copy mesh data
            for (int i = 0; i < _meshesToCopy.Length && i < ghost.meshFilters.Length; i++)
            {
                Mesh mesh = new Mesh();
                _meshesToCopy[i].BakeMesh(mesh);
                ghost.meshFilters[i].mesh = mesh;
            }

            foreach (MeshRenderer meshRenderer in ghost.meshRenderers)
            {
                foreach (Material material in meshRenderer.materials)
                {
                    Color startColor = colorGradient.Evaluate(0f);
                    material.color = startColor;
                }
            }

            _activeGhosts.Add(ghost);
        }

        private void CreateGhostInstance()
        {
            GameObject ghostObj = new GameObject("Ghost");
            ghostObj.transform.SetParent(_ghostContainer);
            ghostObj.SetActive(false);

            GhostInstance ghost = new GhostInstance
            {
                gameObject = ghostObj,
                transform = ghostObj.transform,
                meshFilters = new MeshFilter[_meshesToCopy.Length],
                meshRenderers = new MeshRenderer[_meshesToCopy.Length],
            };


            // Create mesh filter and renderer for each mesh to copy
            for (int i = 0; i < _meshesToCopy.Length; i++)
            {
                GameObject meshObj = new GameObject($"Mesh_{i}");
                meshObj.transform.SetParent(ghostObj.transform);

                // Each SkinMeshRenderer has its own local rotation which must correspond to this object
                meshObj.transform.localPosition = Vector3.zero;
                meshObj.transform.localRotation = _meshesToCopy[i].transform.localRotation;

                MeshFilter mf = meshObj.AddComponent<MeshFilter>();
                MeshRenderer mr = meshObj.AddComponent<MeshRenderer>();

                // Create instance of overdrive trail material
                Material matInstance = new Material(_overdriveTrailMaterial);
                mr.material = matInstance;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;

                Material[] overdriveMaterials = new Material[_meshesToCopy[i].materials.Length];
                for (int j = 0; j < _meshesToCopy[i].materials.Length; j++)
                {
                    overdriveMaterials[j] = matInstance;
                }
                mr.materials = overdriveMaterials;

                ghost.meshFilters[i] = mf;
                ghost.meshRenderers[i] = mr;
            }

            _ghostPool.Enqueue(ghost);
        }

        private GhostInstance GetGhostFromPool()
        {
            if (_ghostPool.Count > 0)
            {
                return _ghostPool.Dequeue();
            }

            // Pool exhausted, create new ghost (shouldn't happen often with proper maxGhosts setting)
            CreateGhostInstance();
            return _ghostPool.Count > 0 ? _ghostPool.Dequeue() : null;
        }

        private void ReturnGhostToPool(GhostInstance ghost)
        {
            ghost.gameObject.SetActive(false);
            ghost.lifetime = 0f;

            // Destroy the baked meshes to free memory
            foreach (MeshFilter mf in ghost.meshFilters)
            {
                if (mf.mesh != null)
                {
                    Destroy(mf.mesh);
                }
            }

            _ghostPool.Enqueue(ghost);
        }
        #endregion

        #region Helper Classes
        private class GhostInstance
        {
            public GameObject gameObject;
            public Transform transform;
            public MeshFilter[] meshFilters;
            public MeshRenderer[] meshRenderers;
            // for materials, see each mesh renderer in meshRenderers
            public float lifetime;
        }
        #endregion
    }
}
