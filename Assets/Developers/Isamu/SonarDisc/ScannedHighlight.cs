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
                _skinRenderer.OnNewSkinSpawned += OnSkinSpawned;

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
                _skinRenderer.OnNewSkinSpawned -= OnSkinSpawned;
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
                GameObject[] shells = new GameObject[_meshRenderers.Length];
                Material[] materials = new Material[_meshRenderers.Length];

                for (int j = 0; j < _meshRenderers.Length; j++)
                {
                    Mesh bakedMesh = new Mesh();
                    _meshRenderers[j].BakeMesh(bakedMesh);

                    GameObject shell = new GameObject("SonarRevealShell");
                    shell.transform.SetPositionAndRotation(_meshRenderers[j].transform.position, _meshRenderers[j].transform.rotation);
                    shell.transform.localScale = _meshRenderers[j].transform.lossyScale;

                    shell.AddComponent<MeshFilter>().mesh = bakedMesh;
                    Material mat = new Material(sonarRevealMaterial);
                    shell.AddComponent<MeshRenderer>().material = mat;

                    shells[j] = shell;
                    materials[j] = mat;
                }

#if !UNITY_SERVER
                if (snapshotEvent != null && snapshotEvent.IsValid())
                    snapshotEvent.Post(gameObject);
#endif

                float elapsed = 0f;
                while (elapsed < snapshotDuration)
                {
                    elapsed += Time.deltaTime;
                    float normalizedTime = Mathf.Clamp01(elapsed / snapshotDuration);

                    foreach (Material mat in materials)
                        mat.SetFloat(RevealTimeID, normalizedTime);

                    yield return null;
                }

                for (int j = 0; j < shells.Length; j++)
                {
                    Destroy(materials[j]);
                    Destroy(shells[j].GetComponent<MeshFilter>().mesh);
                    Destroy(shells[j]);
                }

                if (i < snapshotCount - 1)
                    yield return new WaitForSeconds(snapshotInterval);
            }
        }
    }
}
