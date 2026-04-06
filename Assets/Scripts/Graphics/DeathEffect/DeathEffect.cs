using System.Collections;
using Resonance.Combat.Weapons.Enums;
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
        private PlayerState _playerState;
        private TargetDummy _targetDummy;
        private PlayerSkinRenderer _skinRenderer;
        private SkinnedMeshRenderer[] _meshRenderers;

        private void Awake()
        {
            _playerStats = GetComponentInParent<PlayerStats>();
            _playerState = GetComponentInParent<PlayerState>();
            _targetDummy = GetComponentInParent<TargetDummy>();
            _skinRenderer = GetComponentInParent<PlayerSkinRenderer>();
        }

        private void Start()
        {
            if (_skinRenderer != null)
            {
                _skinRenderer.OnNewSkinSpawned += OnSkinSpawned;

                if (_skinRenderer.CurrentMeshInstance != null)
                    OnSkinSpawned(_skinRenderer.CurrentMeshInstance);
            }
            else
            {
                _meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            }
        }

        private void OnEnable()
        {
            if (_playerStats != null)
            {
                _playerStats.OnPlayerDeath += PlayDeathEffect;
                _playerStats.OnPlayerRespawn += ResetEffect;
            }

            if (_targetDummy != null)
            {
                _targetDummy.OnDeath += PlayDeathEffect;
                _targetDummy.OnRespawn += ResetEffect;
            }

            if (_playerState != null)
                _playerState.OnWeaponClassChanged += OnWeaponClassChanged;
        }

        private void OnDisable()
        {
            if (_playerStats != null)
            {
                _playerStats.OnPlayerDeath -= PlayDeathEffect;
                _playerStats.OnPlayerRespawn -= ResetEffect;
            }

            if (_targetDummy != null)
            {
                _targetDummy.OnDeath -= PlayDeathEffect;
                _targetDummy.OnRespawn -= ResetEffect;
            }

            if (_playerState != null)
                _playerState.OnWeaponClassChanged -= OnWeaponClassChanged;
        }

        private void OnDestroy()
        {
            if (_skinRenderer != null)
                _skinRenderer.OnNewSkinSpawned -= OnSkinSpawned;
        }

        private void OnSkinSpawned(GameObject skinRoot)
        {
            if (_skinRenderer != null && _skinRenderer.ShouldRenderArmsOnly)
            {
                GameObject activeArms = null;
                foreach (var kvp in _skinRenderer.FPArmsInstances)
                {
                    if (kvp.Value != null && kvp.Value.activeSelf)
                    {
                        activeArms = kvp.Value;
                        break;
                    }
                }

                if (activeArms != null)
                    _meshRenderers = activeArms.GetComponentsInChildren<SkinnedMeshRenderer>();
            }
            else
            {
                _meshRenderers = skinRoot.GetComponentsInChildren<SkinnedMeshRenderer>();
            }
        }

        private void OnWeaponClassChanged(WeaponClass newClass)
        {
            if (_skinRenderer.CurrentMeshInstance != null)
                OnSkinSpawned(_skinRenderer.CurrentMeshInstance);
        }

        private void PlayDeathEffect()
        {
            StartCoroutine(GlitchSequence());
        }

        private void ResetEffect()
        {
            if (_meshRenderers == null) return;

            foreach (SkinnedMeshRenderer meshRenderer in _meshRenderers)
            {
                if (meshRenderer != null)
                    meshRenderer.enabled = true;
            }
        }

        private IEnumerator GlitchSequence()
        {
            if (_skinRenderer != null && _skinRenderer.ShouldRenderArmsOnly)
            {
                GameObject activeArms = null;
                foreach (var kvp in _skinRenderer.FPArmsInstances)
                {
                    if (kvp.Value != null && kvp.Value.activeSelf)
                    {
                        activeArms = kvp.Value;
                        break;
                    }
                }

                if (activeArms != null)
                {
                    SkinnedMeshRenderer[] freshRenderers = activeArms.GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (SkinnedMeshRenderer smr in freshRenderers)
                    {
                        smr.enabled = false;
                    }
                    StartCoroutine(RunGlitchGhost(activeArms));
                }
            }
            else
            {
                if (_skinRenderer?.CurrentMeshInstance == null)
                {
                    Debug.LogWarning("[DeathEffect] No mesh instance found, skipping effect.");
                    yield break;
                }

                SkinnedMeshRenderer[] freshRenderers = _skinRenderer.CurrentMeshInstance.GetComponentsInChildren<SkinnedMeshRenderer>();

                foreach (SkinnedMeshRenderer meshRenderer in freshRenderers)
                {
                    meshRenderer.enabled = false;
                }

                StartCoroutine(RunGlitchGhost(_skinRenderer.CurrentMeshInstance));
            }

            yield return null;
        }

        private IEnumerator RunGlitchGhost(GameObject source)
        {
            if (deathGlitchMaterial == null)
            {
                Debug.LogError("[DeathEffect] Death glitch material not assigned.");
                yield break;
            }

            GameObject ghost = Instantiate(source, source.transform.parent);
            ghost.name = "DeathGlitchGhost";
            ghost.transform.localPosition = source.transform.localPosition;
            ghost.transform.localRotation = source.transform.localRotation;
            ghost.transform.localScale = source.transform.localScale;

            Animator ghostAnimator = ghost.GetComponent<Animator>();
            if (ghostAnimator != null)
                ghostAnimator.enabled = false;

            Material material = new Material(deathGlitchMaterial);

            foreach (SkinnedMeshRenderer smr in ghost.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                Material[] mats = new Material[smr.materials.Length];
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = material;
                }
                smr.materials = mats;
            }

            float elapsed = 0f;
            while (elapsed < effectDuration)
            {
                elapsed += Time.deltaTime;
                material.SetFloat(GlitchTimeID, elapsed / effectDuration);
                yield return null;
            }

            Destroy(material);
            Destroy(ghost);
        }
    }
}