using System.Collections;
using PurrNet;
using UnityEngine;
using Resonance.Player;
using Resonance.Entities;

namespace Resonance.VFX
{
    public class DeathEffect : NetworkBehaviour
    {
        [SerializeField] private Material deathGlitchMaterial;
        [SerializeField] private float effectDuration = 0.5f;

        private static readonly int GlitchTimeID = Shader.PropertyToID("_GlitchTime");

        private PlayerStats _playerStats;
        private TargetDummy _targetDummy;
        private SkinnedMeshRenderer[] _meshRenderers;

        private void Awake()
        {
            _playerStats   = GetComponentInParent<PlayerStats>();
            _targetDummy   = GetComponentInParent<TargetDummy>();
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
            RpcPlayDeathEffect();
        }

        [ObserversRpc]
        private void RpcPlayDeathEffect()
        {
            StartCoroutine(GlitchSequence());
        }

        private void ResetEffect()
        {
            foreach (SkinnedMeshRenderer meshRenderer in _meshRenderers)
            {
                meshRenderer.enabled = true;
            }
        }

        private IEnumerator GlitchSequence()
        {
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
