using System.Collections;
using Resonance.Combat.Weapons.Enums;
using UnityEngine;
using Resonance.Player;
using Resonance.Entities;
using Resonance.PlayerController;

namespace Resonance.VFX
{
    [DefaultExecutionOrder(1)]
    public class DeathEffect : MonoBehaviour
    {
        [SerializeField] private Material deathGlitchMaterial;
        [SerializeField] private float effectDuration = 0.5f;

        private static readonly int GlitchTimeID = Shader.PropertyToID("_GlitchTime");

        private PlayerStats _playerStats;
        private PlayerState _playerState;
        private TargetDummy _targetDummy;
        private PlayerSkinRenderer _skinRenderer;
        private SkinnedMeshRenderer[] _skinnedMeshRenderers;

        private void Awake()
        {
            _playerStats = GetComponent<PlayerStats>();
            _playerState = GetComponent<PlayerState>();
            _targetDummy = GetComponent<TargetDummy>();
            _skinRenderer = GetComponent<PlayerSkinRenderer>();
        }

        private void Start()
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

            if (_skinRenderer != null)
            {
                _skinRenderer.OnNewSkinSpawned.AddListener(OnSkinSpawned);

                if (_skinRenderer.CurrentMeshInstance != null)
                    OnSkinSpawned(_skinRenderer.CurrentMeshInstance);
            }
            else
            {
                _skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            }
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
                _skinRenderer.OnNewSkinSpawned?.RemoveListener(OnSkinSpawned);
        }

        private void OnSkinSpawned(GameObject skinRoot)
        {
            if (_skinRenderer != null && _skinRenderer.ShouldRenderArmsOnlyBasedOnCachedMatchState)
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
                    _skinnedMeshRenderers = activeArms.GetComponentsInChildren<SkinnedMeshRenderer>();
            }
            else
            {
                _skinnedMeshRenderers = skinRoot.GetComponentsInChildren<SkinnedMeshRenderer>();
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
            if (_skinRenderer?.CurrentMeshInstance != null)
            {
                foreach (SkinnedMeshRenderer smr in _skinRenderer.CurrentMeshInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    smr.enabled = true;
                }
            }

            if (_skinnedMeshRenderers == null) return;
            foreach (SkinnedMeshRenderer meshRenderer in _skinnedMeshRenderers)
            {
                if (meshRenderer != null)
                {
                    meshRenderer.enabled = true;
                }
            }
        }

        private IEnumerator GlitchSequence()
        {
            if (_skinRenderer != null && _skinRenderer.ShouldRenderArmsOnlyBasedOnCachedMatchState)
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
                    _skinnedMeshRenderers = activeArms.GetComponentsInChildren<SkinnedMeshRenderer>();
                    StartCoroutine(RunGlitchGhost(activeArms));
                    foreach (SkinnedMeshRenderer smr in _skinnedMeshRenderers)
                        smr.enabled = false;
                }
            }
            else
            {
                GameObject tpBody = _skinRenderer?.CurrentMeshInstance;

                if (tpBody == null)
                {
                    if (_skinnedMeshRenderers == null || _skinnedMeshRenderers.Length == 0)
                    {
                        Debug.LogWarning("[DeathEffect] No mesh instance or renderers found for remote player.");
                        yield break;
                    }

                    foreach (SkinnedMeshRenderer smr in _skinnedMeshRenderers)
                        smr.enabled = false;

                    yield break;
                }

                _skinnedMeshRenderers = tpBody.GetComponentsInChildren<SkinnedMeshRenderer>();
                StartCoroutine(RunGlitchGhost(tpBody));

                foreach (SkinnedMeshRenderer smr in _skinnedMeshRenderers)
                    smr.enabled = false;
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
            {
                ghostAnimator.enabled = false;
            }

            Material material = new Material(deathGlitchMaterial);

            foreach (SkinnedMeshRenderer smr in ghost.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!smr.gameObject.activeInHierarchy) continue;

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
