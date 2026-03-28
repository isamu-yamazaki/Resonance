using System.Collections;
using UnityEngine;

namespace Resonance.Combat.Weapons
{
    [RequireComponent(typeof(Light))]
    public class MuzzleFlash : MonoBehaviour
    {
        [SerializeField] private MuzzleFlashSettings settings;

        private ParticleSystem _coreFlash;
        private ParticleSystem _sparks;
        private ParticleSystem _smoke;
        private Light _flashLight;
        private Coroutine _lightCoroutine;
        private bool _built;

        private void Awake()
        {
            _flashLight = GetComponent<Light>();
            _flashLight.enabled = false;
            _flashLight.type = LightType.Point;

            if (settings != null)
                Build();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public void Play()
        {
            if (!_built) return;

            _coreFlash.Play();
            _sparks.Play();
            if (settings.emitSmoke && _smoke != null) _smoke.Play();

            if (_lightCoroutine != null) StopCoroutine(_lightCoroutine);
            _lightCoroutine = StartCoroutine(FlashLight());
        }

        public void ApplySettings(MuzzleFlashSettings newSettings)
        {
            if (newSettings == null) return;
            settings = newSettings;

            if (!_built)
            {
                Build();
                return;
            }

            RefreshParticles();
        }

        // ─── Build ────────────────────────────────────────────────────────────────

        private void Build()
        {
            BuildCoreFlash();
            BuildSparks();
            if (settings.emitSmoke) BuildSmoke();
            _built = true;
        }

        private void BuildCoreFlash()
        {
            var go = new GameObject("CoreFlash");
            go.transform.SetParent(transform, false);
            _coreFlash = go.AddComponent<ParticleSystem>();

            var main = _coreFlash.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = settings.flashDuration;
            main.startLifetime = settings.flashDuration;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.003f, 0.006f);
            main.startColor = settings.flashColor;
            main.gravityModifier = 0f;
            main.maxParticles = 8;

            var emission = _coreFlash.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

            // Radial burst — particles fire outward in all directions from the muzzle
            var shape = _coreFlash.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.001f;

            var renderer = _coreFlash.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 6f;
            renderer.material = CreateAdditiveMaterial(settings.flashColor);
        }

        private void BuildSparks()
        {
            var go = new GameObject("Sparks");
            go.transform.SetParent(transform, false);
            _sparks = go.AddComponent<ParticleSystem>();

            var main = _sparks.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.05f;
            main.startLifetime = settings.sparkLifetime;
            main.startSpeed = new ParticleSystem.MinMaxCurve(settings.sparkSpeed * 0.5f, settings.sparkSpeed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.003f, 0.008f);
            main.startColor = new Color(1f, 0.9f, 0.5f);
            main.gravityModifier = 0.3f;
            main.maxParticles = settings.sparkCount;

            var emission = _sparks.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)settings.sparkCount) });

            var shape = _sparks.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = settings.sparkSpread;
            shape.radius = 0.01f;

            var trails = _sparks.trails;
            trails.enabled = true;
            trails.ratio = 1f;
            trails.lifetime = new ParticleSystem.MinMaxCurve(0.05f);
            trails.minVertexDistance = 0.005f;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 1f), new Keyframe(1f, 0f)
            ));

            var renderer = _sparks.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 4f;
            renderer.material = CreateAdditiveMaterial(new Color(1f, 0.85f, 0.4f));
            renderer.trailMaterial = CreateAdditiveMaterial(new Color(1f, 0.7f, 0.2f));
        }

        private void BuildSmoke()
        {
            var go = new GameObject("Smoke");
            go.transform.SetParent(transform, false);
            _smoke = go.AddComponent<ParticleSystem>();

            var main = _smoke.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.1f;
            main.startLifetime = settings.smokeLifetime;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(settings.smokeStartSize, settings.smokeStartSize * 1.5f);
            main.startColor = new Color(0.5f, 0.5f, 0.5f, 0.15f);
            main.maxParticles = settings.smokeCount;

            var emission = _smoke.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)settings.smokeCount) });

            var shape = _smoke.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.01f;

            var sizeOverLife = _smoke.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, settings.smokeStartSize),
                new Keyframe(1f, settings.smokeEndSize)
            ));

            var colorOverLife = _smoke.colorOverLifetime;
            colorOverLife.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.gray, 0f), new GradientColorKey(Color.gray, 1f) },
                new[] { new GradientAlphaKey(0.15f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLife.color = gradient;

            var renderer = _smoke.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateAlphaMaterial();
        }

        // ─── Refresh (settings hot-swap) ──────────────────────────────────────────

        private void RefreshParticles()
        {
            if (_coreFlash != null)
            {
                var main = _coreFlash.main;
                main.startSize = new ParticleSystem.MinMaxCurve(0.003f, 0.006f);
                main.startLifetime = settings.flashDuration;
                main.startColor = settings.flashColor;
            }

            if (_sparks != null)
            {
                var main = _sparks.main;
                main.startSpeed = new ParticleSystem.MinMaxCurve(settings.sparkSpeed * 0.5f, settings.sparkSpeed);
                main.startLifetime = settings.sparkLifetime;
                main.maxParticles = settings.sparkCount;

                var emission = _sparks.emission;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)settings.sparkCount) });

                var shape = _sparks.shape;
                shape.angle = settings.sparkSpread;
            }

            _flashLight.intensity = settings.lightIntensity;
            _flashLight.range = settings.lightRange;
            _flashLight.color = settings.lightColor;
        }

        // ─── Light coroutine ──────────────────────────────────────────────────────

        private IEnumerator FlashLight()
        {
            _flashLight.color = settings.lightColor;
            _flashLight.range = settings.lightRange;
            _flashLight.intensity = settings.lightIntensity;
            _flashLight.enabled = true;
            yield return new WaitForSeconds(settings.lightDuration);
            _flashLight.enabled = false;
        }

        // ─── Material helpers ─────────────────────────────────────────────────────

        private static Material CreateAdditiveMaterial(Color color)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"))
            {
                color = color
            };
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_BlendMode", 3f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            return mat;
        }

        private static Material CreateAlphaMaterial()
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_BlendMode", 0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            return mat;
        }
    }
}
