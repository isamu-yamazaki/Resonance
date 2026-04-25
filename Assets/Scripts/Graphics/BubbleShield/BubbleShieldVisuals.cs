using System.Collections;
using UnityEngine;

namespace Resonance.Abilities.BubbleShield
{
    [RequireComponent(typeof(MeshRenderer))]
    public class BubbleShieldVisuals : MonoBehaviour
    {
        [Header("Dissolve Animation")]
        [SerializeField] private float dissolveInDuration  = 0.8f;
        [SerializeField] private float dissolveOutDuration = 0.5f;
        [SerializeField] private AnimationCurve spawnCurve   = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve despawnCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("Hit Flash")]
        [SerializeField] private float hitFlashDuration = 0.18f;
        
        [Header("Audio")]
        [SerializeField] private AK.Wwise.Event shieldUpSoundEvent;
        [SerializeField] private AK.Wwise.Event shieldDownSoundEvent;

        private static readonly int PID_Dissolve = Shader.PropertyToID("_DissolveProgress");
        private static readonly int PID_HitFlash  = Shader.PropertyToID("_HitFlash");

        private MeshRenderer          _renderer;
        private MaterialPropertyBlock _mpb;
        private Coroutine             _dissolveCoroutine;
        private Coroutine             _flashCoroutine;

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            _mpb      = new MaterialPropertyBlock();

            if (spawnCurve == null || spawnCurve.length == 0)
                spawnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

            if (despawnCurve == null || despawnCurve.length == 0)
                despawnCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        }

        private void OnEnable()
        {
            _renderer.enabled = true;
            SetDissolve(0f);
#if !UNITY_SERVER
            shieldUpSoundEvent?.Post(gameObject);
#endif
            PlaySpawnDissolve();
        }

        public void PlaySpawnDissolve()
        {
            if (_dissolveCoroutine != null) StopCoroutine(_dissolveCoroutine);
            _dissolveCoroutine = StartCoroutine(AnimateDissolve(spawnCurve, dissolveInDuration));
        }

        public void PlayDespawnDissolve()
        {
            if (_dissolveCoroutine != null) StopCoroutine(_dissolveCoroutine);
#if !UNITY_SERVER
            shieldDownSoundEvent?.Post(gameObject);
#endif
            _dissolveCoroutine = StartCoroutine(AnimateDespawn());
        }

        public void PlayHitFlash()
        {
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(AnimateHitFlash());
        }

        private IEnumerator AnimateDissolve(AnimationCurve curve, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetDissolve(curve.Evaluate(Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            SetDissolve(curve.Evaluate(1f));
        }

        private IEnumerator AnimateDespawn()
        {
            yield return AnimateDissolve(despawnCurve, dissolveOutDuration);
            _renderer.enabled = false;
        }

        private IEnumerator AnimateHitFlash()
        {
            float riseTime = hitFlashDuration * 0.3f;
            float fadeTime = hitFlashDuration - riseTime;
            float elapsed  = 0f;

            while (elapsed < riseTime)
            {
                elapsed += Time.deltaTime;
                SetFlash(Mathf.Clamp01(elapsed / riseTime));
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                SetFlash(1f - Mathf.Clamp01(elapsed / fadeTime));
                yield return null;
            }
            SetFlash(0f);
        }

        private void SetDissolve(float value)
        {
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(PID_Dissolve, value);
            _renderer.SetPropertyBlock(_mpb);
        }

        private void SetFlash(float value)
        {
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(PID_HitFlash, value);
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}
