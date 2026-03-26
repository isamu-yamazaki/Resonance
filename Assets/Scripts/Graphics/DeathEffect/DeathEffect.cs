using System.Collections;
using UnityEngine;
using Resonance.Player;
using Resonance.Entities;
using Resonance.PlayerController;

namespace Resonance.VFX
{
    public class DeathEffect : MonoBehaviour
    {
        [SerializeField] private Material deathGlitchMaterial;
        [SerializeField] private float effectDuration = 0.5f;

        private static readonly int GlitchTimeID = Shader.PropertyToID("_GlitchTime");

        private PlayerStats _playerStats;
        private TargetDummy _targetDummy;
        private PlayerSkinRenderer _skinRenderer;
        private SkinnedMeshRenderer[] _meshRenderers;

        private void Awake()
        {
            _playerStats  = GetComponentInParent<PlayerStats>();
            _targetDummy  = GetComponentInParent<TargetDummy>();
            _skinRenderer = GetComponentInParent<PlayerSkinRenderer>();
        }

        private void Start()
        {
            if (_skinRenderer != null)
            {
                Debug.Log("[DeathEffect] Found PlayerSkinRenderer, subscribing to OnNewSkinSpawned.");
                _skinRenderer.OnNewSkinSpawned += OnSkinSpawned;

                // In case the skin already spawned before we subscribed
                if (_skinRenderer.CurrentMeshInstance != null)
                {
                    Debug.Log("[DeathEffect] Skin already spawned, grabbing renderers now.");
                    OnSkinSpawned(_skinRenderer.CurrentMeshInstance);
                }
            }
            else
            {
                Debug.Log("[DeathEffect] No PlayerSkinRenderer found, using GetComponentsInChildren.");
                _meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
                Debug.Log($"[DeathEffect] Found {_meshRenderers.Length} renderers.");
            }
        }

        private void OnEnable()
        {
            if (_playerStats != null)
            {
                _playerStats.OnPlayerDeath   += PlayDeathEffect;
                _playerStats.OnPlayerRespawn += ResetEffect;
            }

            if (_targetDummy != null)
            {
                _targetDummy.OnDeath   += PlayDeathEffect;
                _targetDummy.OnRespawn += ResetEffect;
            }
        }

        private void OnDisable()
        {
            if (_playerStats != null)
            {
                _playerStats.OnPlayerDeath   -= PlayDeathEffect;
                _playerStats.OnPlayerRespawn -= ResetEffect;
            }

            if (_targetDummy != null)
            {
                _targetDummy.OnDeath   -= PlayDeathEffect;
                _targetDummy.OnRespawn -= ResetEffect;
            }
        }

        private void OnDestroy()
        {
            if (_skinRenderer != null)
            {
                _skinRenderer.OnNewSkinSpawned -= OnSkinSpawned;
            }
        }

        private void OnSkinSpawned(GameObject skinRoot)
        {
            _meshRenderers = skinRoot.GetComponentsInChildren<SkinnedMeshRenderer>();
            Debug.Log($"[DeathEffect] OnSkinSpawned - found {_meshRenderers.Length} renderers on {skinRoot.name}");
        }

        private void PlayDeathEffect()
        {
            Debug.Log($"[DeathEffect] PlayDeathEffect called - meshRenderers: {(_meshRenderers == null ? "null" : _meshRenderers.Length.ToString())}");
            StartCoroutine(GlitchSequence());
        }

        private void ResetEffect()
        {
            if (_meshRenderers == null) return;

            foreach (SkinnedMeshRenderer meshRenderer in _meshRenderers)
            {
                if (meshRenderer != null)
                {
                    meshRenderer.enabled = true;
                }
            }
        }

        private IEnumerator GlitchSequence()
        {
            if (_meshRenderers == null || _meshRenderers.Length == 0)
            {
                Debug.LogWarning("[DeathEffect] No mesh renderers found, skipping effect.");
                yield break;
            }

            foreach (SkinnedMeshRenderer meshRenderer in _meshRenderers)
            {
                meshRenderer.enabled = false;
            }

            foreach (SkinnedMeshRenderer meshRenderer in _meshRenderers)
            {
                Mesh bakedMesh = new Mesh();
                meshRenderer.BakeMesh(bakedMesh);
                StartCoroutine(RunGlitchGhost(bakedMesh, meshRenderer.transform));
            }

            yield return null;
        }

        private IEnumerator RunGlitchGhost(Mesh bakedMesh, Transform sourceTransform)
        {
            if (deathGlitchMaterial == null)
            {
                Debug.LogError("[DeathEffect] Death glitch material not assigned.");
                yield break;
            }

            GameObject ghost = new GameObject("DeathGlitchGhost");
            ghost.transform.SetPositionAndRotation(sourceTransform.position, sourceTransform.rotation);
            ghost.transform.localScale = sourceTransform.lossyScale;

            ghost.AddComponent<MeshFilter>().mesh = bakedMesh;
            Material material = new Material(deathGlitchMaterial);
            ghost.AddComponent<MeshRenderer>().material = material;

            float elapsed = 0f;
            while (elapsed < effectDuration)
            {
                elapsed += Time.deltaTime;
                material.SetFloat(GlitchTimeID, elapsed / effectDuration);
                yield return null;
            }

            Destroy(material);
            Destroy(bakedMesh);
            Destroy(ghost);
        }
    }
}
