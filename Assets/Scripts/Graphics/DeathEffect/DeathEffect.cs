using System.Collections;
using System.Linq;
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
        private MeshRenderer[] _meshRenderersNonSkinned;

        private void Awake()
        {
            _playerStats  = GetComponentInParent<PlayerStats>();
            _playerState = GetComponentInParent<PlayerState>();
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
            Debug.Log($"[DeathEffect] OnEnable - ShouldRenderArmsOnly: {_skinRenderer?.ShouldRenderArmsOnly}");
            
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

            if (_playerState != null && _skinRenderer != null && _skinRenderer.ShouldRenderArmsOnly)
            {
                _playerState.OnWeaponClassChanged += OnWeaponClassChanged;
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
            
            if (_playerState != null)
            {
                _playerState.OnWeaponClassChanged -= OnWeaponClassChanged;
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
                    _meshRenderers = activeArms.GetComponentsInChildren<SkinnedMeshRenderer>();
                    _meshRenderersNonSkinned = activeArms.GetComponentsInChildren<MeshRenderer>();
                }
                
                Debug.Log($"[DeathEffect] OnSkinSpawned - ShouldRenderArmsOnly: {_skinRenderer.ShouldRenderArmsOnly}, activeArms found: {activeArms != null}, skinnedCount: {_meshRenderers?.Length ?? 0}");
            }
            else
            {
                _meshRenderers = skinRoot.GetComponentsInChildren<SkinnedMeshRenderer>();
                _meshRenderersNonSkinned = skinRoot.GetComponentsInChildren<MeshRenderer>();
            }
        }
        
        private void OnWeaponClassChanged(WeaponClass newClass)
        {
            Debug.Log($"[DeathEffect] OnWeaponClassChanged - newClass: {newClass}");
            
            if (_skinRenderer.CurrentMeshInstance != null)
                OnSkinSpawned(_skinRenderer.CurrentMeshInstance);
        }
        
        private void PlayDeathEffect()
        {
            Debug.Log($"[DeathEffect] PlayDeathEffect called - meshRenderers: {(_meshRenderers == null ? "null" : _meshRenderers.Length.ToString())}");
            StartCoroutine(GlitchSequence());
        }

        private void ResetEffect()
        {
            if (_skinRenderer != null && _skinRenderer.ShouldRenderArmsOnly)
            {
                if (_meshRenderers != null)
                {
                    foreach (SkinnedMeshRenderer meshRenderer in _meshRenderers)
                    {
                        if (meshRenderer != null)
                            meshRenderer.enabled = true;
                    }
                }

                if (_meshRenderersNonSkinned != null)
                {
                    foreach (MeshRenderer meshRenderer in _meshRenderersNonSkinned)
                    {
                        if (meshRenderer != null)
                            meshRenderer.enabled = true;
                    }
                }

                return;
            }

            if (_meshRenderers != null)
            {
                foreach (SkinnedMeshRenderer meshRenderer in _meshRenderers)
                {
                    if (meshRenderer != null)
                        meshRenderer.enabled = true;
                }
            }

            if (_meshRenderersNonSkinned != null)
            {
                foreach (MeshRenderer meshRenderer in _meshRenderersNonSkinned)
                {
                    if (meshRenderer != null)
                        meshRenderer.enabled = true;
                }
            }
        }

        private IEnumerator GlitchSequence()
        {
            if ((_meshRenderers == null || _meshRenderers.Length == 0) && 
                (_meshRenderersNonSkinned == null || _meshRenderersNonSkinned.Length == 0))
            {
                Debug.LogWarning("[DeathEffect] No mesh renderers found, skipping effect.");
                yield break;
            }

            Debug.Log($"[DeathEffect] GlitchSequence - skinnedCount: {_meshRenderers?.Length ?? 0}, nonSkinnedCount: {_meshRenderersNonSkinned?.Length ?? 0}");

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
                    StartCoroutine(RunFPGlitchGhost(activeArms));

                foreach (SkinnedMeshRenderer meshRenderer in _meshRenderers)
                {
                    meshRenderer.enabled = false;
                }
            }
            else
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

                if (_meshRenderersNonSkinned != null)
                {
                    foreach (MeshRenderer meshRenderer in _meshRenderersNonSkinned)
                    {
                        if (meshRenderer == null) continue;
                        MeshFilter mf = meshRenderer.GetComponent<MeshFilter>();
                        if (mf != null && mf.mesh != null)
                            StartCoroutine(RunGlitchGhost(mf.mesh, meshRenderer.transform));
                        meshRenderer.enabled = false;
                    }
                }
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
            Debug.Log($"[DeathEffect] RunGlitchGhost - bakedMesh vertices: {bakedMesh.vertexCount}, position: {sourceTransform.position}");

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
        
        private IEnumerator RunFPGlitchGhost(GameObject armsSource)
        {
            if (deathGlitchMaterial == null)
            {
                Debug.LogError("[DeathEffect] Death glitch material not assigned.");
                yield break;
            }

            GameObject ghost = Instantiate(armsSource, armsSource.transform.parent);
            ghost.name = "DeathGlitchGhost";
            ghost.transform.localPosition = armsSource.transform.localPosition;
            ghost.transform.localRotation = armsSource.transform.localRotation;
            ghost.transform.localScale = armsSource.transform.localScale;
            
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
