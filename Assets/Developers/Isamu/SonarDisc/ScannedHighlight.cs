using System.Collections;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Abilities.SonarDisc
{
    /// <summary>
    /// Added to a detected player on the owner's client only.
    /// Fires a configurable number of sequential scan snapshots, each baking the
    /// player's current pose and displaying a shell with the SonarReveal shader.
    /// Each snapshot replaces the previous one.
    /// </summary>
    public class ScannedHighlight : MonoBehaviour
    {
        [SerializeField] private float snapshotDuration = 1f;
        [SerializeField] private float snapshotInterval = 1f;
        [SerializeField] private int snapshotCount = 3;

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

        private void OnDestroy()
        {
            if (_skinRenderer != null)
                _skinRenderer.OnNewSkinSpawned -= OnSkinSpawned;
        }

        private void OnSkinSpawned(GameObject skinRoot)
        {
            _meshRenderers = skinRoot.GetComponentsInChildren<SkinnedMeshRenderer>();
        }

        public void Play(Material revealMaterial)
        {
            if (revealMaterial == null)
            {
                Debug.LogWarning("[ScannedHighlight] revealMaterial is not assigned.");
                return;
            }

            // Populate renderers if Start hasn't run yet
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

            StartCoroutine(SnapshotSequence(revealMaterial));
        }

        private IEnumerator SnapshotSequence(Material revealMaterial)
        {
            for (int i = 0; i < snapshotCount; i++)
            {
                // Bake current pose
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
                    Material mat = new Material(revealMaterial);
                    shell.AddComponent<MeshRenderer>().material = mat;

                    shells[j] = shell;
                    materials[j] = mat;
                }

                // Drive _RevealTime 0 → 1 over snapshotDuration
                float elapsed = 0f;
                while (elapsed < snapshotDuration)
                {
                    elapsed += Time.deltaTime;
                    float normalizedTime = Mathf.Clamp01(elapsed / snapshotDuration);

                    foreach (Material mat in materials)
                        mat.SetFloat(RevealTimeID, normalizedTime);

                    yield return null;
                }

                // Destroy this snapshot's shells
                for (int j = 0; j < shells.Length; j++)
                {
                    Destroy(materials[j]);
                    Destroy(shells[j].GetComponent<MeshFilter>().mesh);
                    Destroy(shells[j]);
                }

                // Wait before next snapshot (skip wait after last one)
                if (i < snapshotCount - 1)
                    yield return new WaitForSeconds(snapshotInterval);
            }

            Destroy(this);
        }
    }
}
