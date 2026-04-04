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

        [Header("Attenuation")]
        [SerializeField] private float attenuationScale = 0.02f; // lower = slower falloff

        [Header("Mesh")]
        [SerializeField] private Material volumetricMaterial;

        public int      RaymarchSteps    { get => raymarchSteps;    set => raymarchSteps    = value; }
        public float    Density          { get => density;          set => density          = value; }
        public float    JitterStrength   { get => jitterStrength;   set => jitterStrength   = value; }
        public float    MieG             { get => mieG;             set => mieG             = value; }
        public float    AttenuationScale { get => attenuationScale; set => attenuationScale = value; }
        public Material Material         { get => volumetricMaterial; set => volumetricMaterial = value; }

        // When set, drives proxy mesh length independently from _light.range (which controls attenuation)
        public float? ConeLength { get; set; }

        private Light        _light;
        private GameObject   _proxyMeshObj;
        private MeshFilter   _meshFilter;
        private MeshRenderer _meshRenderer;
        private MaterialPropertyBlock _mpb;

        private static readonly int ID_LightColor       = Shader.PropertyToID("_LightColor");
        private static readonly int ID_LightIntensity   = Shader.PropertyToID("_LightIntensity");
        private static readonly int ID_LightPosWS       = Shader.PropertyToID("_LightPosWS");
        private static readonly int ID_LightDirWS       = Shader.PropertyToID("_LightDirWS");
        private static readonly int ID_ConeAngleCos     = Shader.PropertyToID("_ConeAngleCos");
        private static readonly int ID_ConeRange        = Shader.PropertyToID("_ConeRange");
        private static readonly int ID_AttenuationScale = Shader.PropertyToID("_AttenuationScale");
        private static readonly int ID_MieG             = Shader.PropertyToID("_MieG");
        private static readonly int ID_Density          = Shader.PropertyToID("_Density");
        private static readonly int ID_Steps            = Shader.PropertyToID("_RaymarchSteps");
        private static readonly int ID_Jitter           = Shader.PropertyToID("_JitterStrength");

        private void OnEnable()
        {
            _light = GetComponent<Light>();
            _mpb   = new MaterialPropertyBlock();
            if (volumetricMaterial != null)
                BuildProxyMesh();
        }

        private void OnDisable()
        {
            DestroyProxyMesh();
        }

        private void LateUpdate()
        {
            if (_light == null || _light.type != LightType.Spot) return;

            // Build deferred if material was set after OnEnable (e.g. set via code before SetActive)
            if (_proxyMeshObj == null && volumetricMaterial != null)
                BuildProxyMesh();

            UpdateProxyTransform();
            UpdateMaterialProperties();
        }

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

            _meshFilter.sharedMesh       = BuildConeMesh(32);
            _meshRenderer.sharedMaterial = volumetricMaterial;
            _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _meshRenderer.receiveShadows    = false;
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

        private void UpdateProxyTransform()
        {
            if (_proxyMeshObj == null) return;

            float halfAngle  = _light.spotAngle * 0.5f * Mathf.Deg2Rad;
            float meshLength = ConeLength ?? _light.range; // ConeLength overrides range for mesh only
            float baseRadius = Mathf.Tan(halfAngle) * meshLength;

            _proxyMeshObj.transform.position   = transform.position;
            _proxyMeshObj.transform.rotation   = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(90f, 0f, 0f);
            _proxyMeshObj.transform.localScale  = new Vector3(baseRadius, meshLength * 0.5f, baseRadius);
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
            _mpb.SetFloat(ID_ConeRange,        ConeLength.HasValue ? ConeLength.Value : _light.range);
            _mpb.SetFloat(ID_AttenuationScale, attenuationScale);
            _mpb.SetFloat(ID_MieG,           mieG);
            _mpb.SetFloat(ID_Density,        density);
            _mpb.SetInt(ID_Steps,            raymarchSteps);
            _mpb.SetFloat(ID_Jitter,         jitterStrength);
            _meshRenderer.SetPropertyBlock(_mpb);
        }

        // Cone with tip at origin, base at +Y, radius 1 at base — scaled at runtime
        private static Mesh BuildConeMesh(int segments)
        {
            var mesh = new Mesh { name = "VolumetricLightCone" };

            var verts = new Vector3[segments + 2];
            var tris  = new int[segments * 3 * 2];

            verts[0] = Vector3.zero; // tip
            for (int i = 0; i < segments; i++)
            {
                float a = (float)i / segments * Mathf.PI * 2f;
                verts[i + 1] = new Vector3(Mathf.Cos(a), 1f, Mathf.Sin(a));
            }
            verts[segments + 1] = new Vector3(0f, 1f, 0f); // base center

            int t = 0;
            for (int i = 0; i < segments; i++) // sides
            {
                tris[t++] = 0;
                tris[t++] = (i + 1) % segments + 1;
                tris[t++] = i + 1;
            }
            for (int i = 0; i < segments; i++) // base cap
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
            if (_proxyMeshObj != null)
            {
                UpdateProxyTransform();
                UpdateMaterialProperties();
            }
        }
#endif
    }
}
