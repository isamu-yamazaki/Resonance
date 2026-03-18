using UnityEngine;
using System.Collections;

public class EliminationPopupController : MonoBehaviour
{
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.15f;
    [SerializeField] private float displayTime = 0.25f;

    private PlayerViewModel viewModel;
    
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

        viewModel.GotKill.ChangeEvent += OnGotKill;
    }
    
    private void Awake()
    {
        popupRoot.SetActive(true);
        canvasGroup.alpha = 0f;
    }

    private void OnEnable()
    {
        if (viewModel != null)
            viewModel.GotKill.ChangeEvent += OnGotKill;
    }

    private void OnDisable()
    {
        if (viewModel != null)
            viewModel.GotKill.ChangeEvent -= OnGotKill;
    }

    private void OnGotKill(bool value)
    {
        if (value) ShowPopup();
    }

    private void ShowPopup()
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        yield return Fade(0f, 1f, fadeDuration);
        yield return new WaitForSeconds(displayTime);
        yield return Fade(1f, 0f, fadeDuration);
    }

    private IEnumerator Fade(float start, float end, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, end, time / duration);
            yield return null;
        }
        canvasGroup.alpha = end;
    }
}