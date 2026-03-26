using System.Collections;
using System.Collections.Generic;
using PurrNet;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Abilities.SonarDisc
{
    /// <summary>
    /// Spawns a scaled-out shell mesh over the player and drives the Electrocute
    /// shader on it, creating a lightning aura effect without affecting the player's
    /// own materials.
    /// </summary>
    public class ElectrocuteEffect : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Material electrocuteMaterial;
        [SerializeField] private float effectDuration = 1f;

        private static readonly int ElectrocuteTimeID = Shader.PropertyToID("_ElectrocuteTime");

        private PlayerSkinRenderer _skinRenderer;
        private SkinnedMeshRenderer[] _meshRenderers;
        private bool _isPlaying;

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

        [ObserversRpc(runLocally: true)]
        public void Play()
        {
            if (_isPlaying)
                return;

            if (electrocuteMaterial == null)
            {
                Debug.LogWarning("[ElectrocuteEffect] electrocuteMaterial is not assigned.");
                return;
            }

            if (_meshRenderers == null || _meshRenderers.Length == 0)
            {
                Debug.LogWarning("[ElectrocuteEffect] No SkinnedMeshRenderers found.");
                return;
            }

            StartCoroutine(ElectrocuteSequence());
        }

        private IEnumerator ElectrocuteSequence()
        {
            _isPlaying = true;

            List<(GameObject shell, Material material)> shells = new List<(GameObject, Material)>();

            foreach (SkinnedMeshRenderer skinnedMeshRenderer in _meshRenderers)
            {
                Mesh bakedMesh = new Mesh();
                skinnedMeshRenderer.BakeMesh(bakedMesh);

                GameObject shell = new GameObject("ElectrocuteShell");
                shell.transform.SetPositionAndRotation(skinnedMeshRenderer.transform.position, skinnedMeshRenderer.transform.rotation);
                shell.transform.localScale = skinnedMeshRenderer.transform.lossyScale;

                shell.AddComponent<MeshFilter>().mesh = bakedMesh;
                Material shellMaterial = new Material(electrocuteMaterial);
                shell.AddComponent<MeshRenderer>().material = shellMaterial;

                shells.Add((shell, shellMaterial));
            }

            float elapsed = 0f;
            while (elapsed < effectDuration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / effectDuration);

                foreach ((GameObject shell, Material material) in shells)
                    material.SetFloat(ElectrocuteTimeID, normalizedTime);

                yield return null;
            }

            foreach ((GameObject shell, Material material) in shells)
            {
                Destroy(material);
                Destroy(shell.GetComponent<MeshFilter>().mesh);
                Destroy(shell);
            }

            _isPlaying = false;
        }
    }
}
