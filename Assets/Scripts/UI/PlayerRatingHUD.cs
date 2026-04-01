using System.Collections;
using UnityEngine;
using TMPro;

public class PlayerRatingHUD : MonoBehaviour
{
     [Header("UI")]
    [SerializeField] private TextMeshProUGUI ratingText;
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI deltaText;

    [Header("Tuning")]
    [SerializeField] private float countUpDuration = 0.8f;
    [SerializeField] private float pulseScale = 1.25f;
    [SerializeField] private float pulseDuration = 0.35f;

    private PlayerViewModel viewModel;
    private float _displayedRating;
    private Coroutine _countUpCoroutine;
    private Coroutine _pulseCoroutine;
    private Coroutine _deltaCoroutine;
    private CanvasGroup _deltaCanvasGroup;
    private Vector3 _deltaStartPos;
    
    private void Start()
    {
        StartCoroutine(WaitForViewModel());
    }
    
    private IEnumerator WaitForViewModel()
    {
        while (viewModel == null)
        {
            viewModel = FindObjectOfType<PlayerViewModel>();
            yield return null;
        }

        _deltaCanvasGroup = deltaText.GetComponent<CanvasGroup>();

        viewModel.Rating.ChangeEvent += OnRatingChanged;
        viewModel.Rank.ChangeEvent += OnRankChanged;
        viewModel.RatingDelta.ChangeEvent += OnDeltaChanged;
        
        _deltaCanvasGroup.alpha = 0f;
        _deltaStartPos = deltaText.rectTransform.anchoredPosition3D;
    }

    private void OnDisable()
    {
        if (viewModel == null) return;

        viewModel.Rating.ChangeEvent -= OnRatingChanged;
        viewModel.Rank.ChangeEvent -= OnRankChanged;
        viewModel.RatingDelta.ChangeEvent -= OnDeltaChanged;
    }

    private void OnRatingChanged(float newRating)
    {
        if (_countUpCoroutine != null)
            StopCoroutine(_countUpCoroutine);
        _countUpCoroutine = StartCoroutine(CountUpTo(newRating));
    }

    private void OnRankChanged(int rank)
    {
        rankText.text = rank > 0 ? $"#{rank}" : "-";
    }

    private void OnDeltaChanged(float delta)
    {
        if (delta == 0f) return;

        if (_deltaCoroutine != null)
            StopCoroutine(_deltaCoroutine);
        _deltaCoroutine = StartCoroutine(ShowDelta(delta));

        if (delta > 0f)
        {
            if (_pulseCoroutine != null)
                StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = StartCoroutine(PulseText());
        }
    }

    private IEnumerator CountUpTo(float target)
    {
        float start = _displayedRating;
        float elapsed = 0f;

        while (elapsed < countUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / countUpDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            _displayedRating = Mathf.Lerp(start, target, eased);
            ratingText.text = Mathf.RoundToInt(_displayedRating).ToString("N0");
            yield return null;
        }

        _displayedRating = target;
        ratingText.text = Mathf.RoundToInt(target).ToString("N0");
    }

    private IEnumerator PulseText()
    {
        // Scale up fast, ease back to normal
        float elapsed = 0f;
        float halfDuration = pulseDuration * 0.35f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float scale = Mathf.Lerp(1f, pulseScale, t);
            ratingText.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        elapsed = 0f;
        float returnDuration = pulseDuration * 0.65f;

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / returnDuration;
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float scale = Mathf.Lerp(pulseScale, 1f, eased);
            ratingText.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        ratingText.transform.localScale = Vector3.one;
    }

    private IEnumerator ShowDelta(float delta)
    {
        deltaText.rectTransform.anchoredPosition3D = _deltaStartPos;
        deltaText.text = delta > 0 ? $"+{Mathf.RoundToInt(delta)}" : Mathf.RoundToInt(delta).ToString();
        deltaText.color = delta > 0 ? Color.green : Color.red;
        _deltaCanvasGroup.alpha = 1f;

        float elapsed = 0f;
        float duration = 1.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            _deltaCanvasGroup.alpha = 1f - t;
            deltaText.rectTransform.anchoredPosition3D = _deltaStartPos + Vector3.up * (t * 30f);
            yield return null;
        }

        _deltaCanvasGroup.alpha = 0f;
        deltaText.rectTransform.anchoredPosition3D = _deltaStartPos;
    }
}