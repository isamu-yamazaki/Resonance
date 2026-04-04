using UnityEngine;

namespace Resonance
{
    [RequireComponent(typeof(Light))]
    [ExecuteAlways]
    public class VolumetricLight : MonoBehaviour
    {
        [Header("Raymarching")]
        [SerializeField] private int   raymarchSteps  = 24;
        [SerializeField] private float density        = 0.3f;
        [SerializeField] private float jitterStrength = 0.5f;

        [Header("Scattering")]
        [SerializeField, Range(-1f, 1f)] private float mieG = 0.3f;

        [Header("Mesh")]
        [SerializeField] private Material volumetricMaterial;

        // Internal
        private Light        _light;
        private GameObject   _proxyMeshObj;
        private MeshFilter   _meshFilter;
        private MeshRenderer _meshRenderer;
        private MaterialPropertyBlock _mpb;

        private static readonly int ID_LightColor     = Shader.PropertyToID("_LightColor");
        private static readonly int ID_LightIntensity = Shader.PropertyToID("_LightIntensity");
        private static readonly int ID_LightPosWS     = Shader.PropertyToID("_LightPosWS");
        private static readonly int ID_LightDirWS     = Shader.PropertyToID("_LightDirWS");
        private static readonly int ID_ConeAngleCos   = Shader.PropertyToID("_ConeAngleCos");
        private static readonly int ID_ConeRange      = Shader.PropertyToID("_ConeRange");
        private static readonly int ID_MieG           = Shader.PropertyToID("_MieG");
        private static readonly int ID_Density        = Shader.PropertyToID("_Density");
        private static readonly int ID_Steps          = Shader.PropertyToID("_RaymarchSteps");
        private static readonly int ID_Jitter         = Shader.PropertyToID("_JitterStrength");

        private void OnEnable()
        {
            _light = GetComponent<Light>();
            _mpb   = new MaterialPropertyBlock();
            BuildProxyMesh();
        }

        private void OnDisable()
        {
            DestroyProxyMesh();
        }

        private void LateUpdate()
        {
            if (_light == null || _light.type != LightType.Spot) return;
            UpdateProxyTransform();
            UpdateMaterialProperties();
        }

        // ------------------------------------------------------------------
        // Build a cone mesh that covers the spotlight volume.
        // Unity's cylinder primitive won't work here — we need a real cone
        // so the proxy tightly wraps the light volume (important for perf).
        // ------------------------------------------------------------------
        private void BuildProxyMesh()
        {
            if (volumetricMaterial == null)
            {
                Debug.LogError("[VolumetricLight] No material assigned!", this);
                return;
            }

            _proxyMeshObj = new GameObject("VolumetricLight_Proxy");
            _proxyMeshObj.hideFlags = HideFlags.HideAndDontSave;

            _meshFilter   = _proxyMeshObj.AddComponent<MeshFilter>();
            _meshRenderer = _proxyMeshObj.AddComponent<MeshRenderer>();

            _meshFilter.sharedMesh      = BuildConeMesh(32);
            _meshRenderer.sharedMaterial = volumetricMaterial;
            _meshRenderer.shadowCastingMode  = UnityEngine.Rendering.ShadowCastingMode.Off;
            _meshRenderer.receiveShadows     = false;
        }

        private void DestroyProxyMesh()
        {
            if (_proxyMeshObj != null)
            {
                if (Application.isPlaying)
                    Destroy(_proxyMeshObj);
                else
                    DestroyImmediate(_proxyMeshObj);
            }
        }

        // Positions proxy to match light transform + cone dimensions
        private void UpdateProxyTransform()
        {
            if (_proxyMeshObj == null) return;

            float halfAngle  = _light.spotAngle * 0.5f * Mathf.Deg2Rad;
            float range      = _light.range;
            float baseRadius = Mathf.Tan(halfAngle) * range;

            // Use world-space position and direction directly to avoid
            // inheriting any parent scale that would squash the cone
            Vector3 worldPos = transform.position;
            Vector3 worldFwd = transform.forward; // already normalized world-space

            _proxyMeshObj.transform.position   = worldPos;
            _proxyMeshObj.transform.rotation   = Quaternion.LookRotation(worldFwd) * Quaternion.Euler(90f, 0f, 0f);
            _proxyMeshObj.transform.localScale  = new Vector3(baseRadius, range * 0.5f, baseRadius);
        }

        private void UpdateMaterialProperties()
        {
            if (_proxyMeshObj == null || _meshRenderer == null) return;

            float halfAngle = _light.spotAngle * 0.5f * Mathf.Deg2Rad;

            _meshRenderer.GetPropertyBlock(_mpb);

            _mpb.SetColor(ID_LightColor,     _light.color);
            _mpb.SetFloat(ID_LightIntensity, _light.intensity);
            _mpb.SetVector(ID_LightPosWS,    transform.position);
            _mpb.SetVector(ID_LightDirWS,    transform.forward);
            _mpb.SetFloat(ID_ConeAngleCos,   Mathf.Cos(halfAngle));
            _mpb.SetFloat(ID_ConeRange,      _light.range);
            _mpb.SetFloat(ID_MieG,           mieG);
            _mpb.SetFloat(ID_Density,        density);
            _mpb.SetInt(ID_Steps,            raymarchSteps);
            _mpb.SetFloat(ID_Jitter,         jitterStrength);

            _meshRenderer.SetPropertyBlock(_mpb);
        }

        // Builds a cone with tip at origin, base at +Y * height
        // Segments = sides of the cone base circle
        private static Mesh BuildConeMesh(int segments)
        {
            Mesh mesh = new Mesh();
            mesh.name = "VolumetricLightCone";

            int vertCount = segments + 2; // tip + base ring + base center
            Vector3[] verts    = new Vector3[vertCount];
            int[]     tris     = new int[segments * 3 * 2]; // sides + base cap

            verts[0] = Vector3.zero; // tip

            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                verts[i + 1] = new Vector3(Mathf.Cos(angle), 1f, Mathf.Sin(angle));
            }

            verts[segments + 1] = new Vector3(0f, 1f, 0f); // base center

            // Side triangles
            int t = 0;
            for (int i = 0; i < segments; i++)
            {
                tris[t++] = 0;
                tris[t++] = (i + 1) % segments + 1;
                tris[t++] = i + 1;
            }

            // Base cap
            for (int i = 0; i < segments; i++)
            {
                tris[t++] = segments + 1;
                tris[t++] = i + 1;
                tris[t++] = (i + 1) % segments + 1;
            }

            mesh.vertices  = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Rebuild mesh if component already live in editor
            if (_proxyMeshObj != null)
            {
                UpdateProxyTransform();
                UpdateMaterialProperties();
            }
        }
#endif
    }
}
