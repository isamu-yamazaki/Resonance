using System.Collections;
using UnityEngine;
using Resonance.Player;
using Resonance.Entities;

namespace Resonance.VFX
{
    public class DeathEffect : MonoBehaviour
    {
        [SerializeField] private Material deathGlitchMaterial;
        [SerializeField] private float effectDuration = 0.5f;

        private static readonly int GlitchTimeID = Shader.PropertyToID("_GlitchTime");

        private PlayerStats _playerStats;
        private TargetDummy _targetDummy;
        private SkinnedMeshRenderer[] _meshRenderers;

        private void Awake()
        {
            _playerStats  = GetComponentInParent<PlayerStats>();
            _targetDummy  = GetComponentInParent<TargetDummy>();
            _meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
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

        private void PlayDeathEffect()
        {
            StartCoroutine(GlitchSequence());
        }

        private IEnumerator GlitchSequence()
        {
            // Hide original mesh immediately
            foreach (SkinnedMeshRenderer meshRenderer in _meshRenderers)
            {
                meshRenderer.enabled = false;
            }

            // Bake each skinned mesh to a static snapshot and spawn a glitch ghost
            foreach (SkinnedMeshRenderer meshRenderer in _meshRenderers)
            {
                Mesh bakedMesh = new Mesh();
                meshRenderer.BakeMesh(bakedMesh);
                SpawnGlitchGhost(bakedMesh, meshRenderer.transform);
            }

            yield return null;
        }

        private void SpawnGlitchGhost(Mesh bakedMesh, Transform sourceTransform)
        {
            if (deathGlitchMaterial == null)
            {
                Debug.LogError("[DeathEffect] Death glitch material not assigned.");
                return;
            }

            GameObject ghost = new GameObject("DeathGlitchGhost");
            ghost.transform.SetPositionAndRotation(sourceTransform.position, sourceTransform.rotation);
            ghost.transform.localScale = sourceTransform.lossyScale;

            ghost.AddComponent<MeshFilter>().mesh = bakedMesh;
            ghost.AddComponent<MeshRenderer>().material = new Material(deathGlitchMaterial);

            StartCoroutine(DriveGlitch(ghost, effectDuration));
        }

        private IEnumerator DriveGlitch(GameObject ghost, float duration)
        {
            Material material = ghost.GetComponent<MeshRenderer>().material;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                material.SetFloat(GlitchTimeID, elapsed / duration);
                yield return null;
            }

            Destroy(material);
            Destroy(ghost.GetComponent<MeshFilter>().mesh);
            Destroy(ghost);
        }

        private void ResetEffect()
        {
            foreach (SkinnedMeshRenderer meshRenderer in _meshRenderers)
            {
                meshRenderer.enabled = true;
            }
        }
    }
}
