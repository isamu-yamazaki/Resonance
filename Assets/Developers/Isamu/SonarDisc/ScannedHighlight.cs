using System.Collections;
using PurrNet;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Abilities.SonarDisc
{
    // Plays scan snapshot VFX on the scanned player. Called via TargetRpc from SonarDiscProjectile.
    public class ScannedHighlight : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Material sonarRevealMaterial;
        [SerializeField] private float snapshotDuration = 1f;
        [SerializeField] private float snapshotInterval = 1f;
        [SerializeField] private int snapshotCount = 3;

#if !UNITY_SERVER
        [Header("Wwise Events")]
        // TODO: Assign scanned snapshot event in inspector
        [SerializeField] private AK.Wwise.Event snapshotEvent;
#endif

        private static readonly int RevealTimeID = Shader.PropertyToID("_RevealTime");

        private PlayerSkinRenderer _skinRenderer;
        private SkinnedMeshRenderer[] _meshRenderers;

        private void Awake()
        {
            _skinRenderer = GetComponentInParent<PlayerSkinRenderer>();
        }

        private void Start()
        {
            if (_skinRenderer != null)
            {
                _skinRenderer.OnNewSkinSpawned.AddListener(OnSkinSpawned);

                if (_skinRenderer.CurrentMeshInstance != null)
                    OnSkinSpawned(_skinRenderer.CurrentMeshInstance);
            }
            else
            {
                _meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            }
        }

        protected override void OnDestroy()
        {
            if (_skinRenderer != null)
                _skinRenderer.OnNewSkinSpawned?.RemoveListener(OnSkinSpawned);
        }

        private void OnSkinSpawned(GameObject skinRoot)
        {
            _meshRenderers = skinRoot.GetComponentsInChildren<SkinnedMeshRenderer>();
        }

        public void Play()
        {
            if (sonarRevealMaterial == null)
            {
                Debug.LogWarning("[ScannedHighlight] sonarRevealMaterial is not assigned.");
                return;
            }

            if (_meshRenderers == null || _meshRenderers.Length == 0)
            {
                if (_skinRenderer != null && _skinRenderer.CurrentMeshInstance != null)
                    _meshRenderers = _skinRenderer.CurrentMeshInstance.GetComponentsInChildren<SkinnedMeshRenderer>();
                else
                    _meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            }

            if (_meshRenderers == null || _meshRenderers.Length == 0)
            {
                Debug.LogWarning("[ScannedHighlight] No SkinnedMeshRenderers found.");
                return;
            }

            StartCoroutine(SnapshotSequence());
        }

        private IEnumerator SnapshotSequence()
        {
            for (int i = 0; i < snapshotCount; i++)
            {
                GameObject source = _skinRenderer.CurrentMeshInstance;
                if (source == null) yield break;

                GameObject snapshot = Instantiate(source, source.transform.parent);
                snapshot.transform.localPosition = source.transform.localPosition;
                snapshot.transform.localRotation = source.transform.localRotation;
                snapshot.transform.localScale = source.transform.localScale;

                Animator snapshotAnimator = snapshot.GetComponent<Animator>();
                if (snapshotAnimator != null)
                    snapshotAnimator.enabled = false;

                Material mat = new Material(sonarRevealMaterial);
                foreach (SkinnedMeshRenderer smr in snapshot.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if (!smr.gameObject.activeInHierarchy) continue;
                    Material[] mats = new Material[smr.materials.Length];
                    for (int k = 0; k < mats.Length; k++)
                        mats[k] = mat;
                    smr.materials = mats;
                }

#if !UNITY_SERVER
                if (snapshotEvent != null && snapshotEvent.IsValid())
                    snapshotEvent.Post(gameObject);
#endif

                float elapsed = 0f;
                while (elapsed < snapshotDuration)
                {
                    elapsed += Time.deltaTime;
                    mat.SetFloat(RevealTimeID, Mathf.Clamp01(elapsed / snapshotDuration));
                    yield return null;
                }

                Destroy(mat);
                Destroy(snapshot);

                if (i < snapshotCount - 1)
                    yield return new WaitForSeconds(snapshotInterval);
            }
        }
    }
}
