using System.Collections;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Abilities.SonarDisc
{
    /// <summary>
    /// Added to a detected player on the owner's client only.
    /// Bakes the player's skin mesh and spawns a shell with the SonarReveal shader,
    /// visible through walls for highlightDuration seconds.
    /// </summary>
    public class ScannedHighlight : MonoBehaviour
    {
        [SerializeField] private float highlightDuration = 3f;

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

            // Populate renderers if Start hasn't run yet (e.g. component was just added)
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

            StartCoroutine(RevealSequence(revealMaterial));
        }

        private IEnumerator RevealSequence(Material sonarRevealMaterial)
        {
            // Bake shell meshes at moment of reveal
            GameObject[] shells = new GameObject[_meshRenderers.Length];
            Material[] materials = new Material[_meshRenderers.Length];

            for (int i = 0; i < _meshRenderers.Length; i++)
            {
                Mesh bakedMesh = new Mesh();
                _meshRenderers[i].BakeMesh(bakedMesh);

                GameObject shell = new GameObject("SonarRevealShell");
                shell.transform.SetPositionAndRotation(_meshRenderers[i].transform.position, _meshRenderers[i].transform.rotation);
                shell.transform.localScale = _meshRenderers[i].transform.lossyScale;

                shell.AddComponent<MeshFilter>().mesh = bakedMesh;
                Material mat = new Material(sonarRevealMaterial);
                shell.AddComponent<MeshRenderer>().material = mat;

                shells[i] = shell;
                materials[i] = mat;
            }

            // Drive _RevealTime 0 → 1 over highlightDuration
            float elapsed = 0f;
            while (elapsed < highlightDuration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / highlightDuration);

                foreach (Material mat in materials)
                    mat.SetFloat(RevealTimeID, normalizedTime);

                yield return null;
            }

            // Cleanup
            for (int i = 0; i < shells.Length; i++)
            {
                Destroy(materials[i]);
                Destroy(shells[i].GetComponent<MeshFilter>().mesh);
                Destroy(shells[i]);
            }

            Destroy(this);
        }
    }
}
