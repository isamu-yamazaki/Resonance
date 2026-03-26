using UnityEngine;
using TMPro;

public class AmmoUI : MonoBehaviour
{
    private PlayerViewModel viewModel;

    [SerializeField] private TextMeshProUGUI ammoText;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color lowColor = new Color(1f, 0.6f, 0f);
    [SerializeField] private Color criticalColor = Color.red;

    [Header("Thresholds")]
    [Tooltip("Ammo at or below this count (or % for large mags) shows orange")]
    [SerializeField] private float lowPercent = 0.5f;       // 50% -> orange
    [SerializeField] private float criticalPercent = 0.25f; // 25% -> red
    [SerializeField] private int criticalMinBullets = 1;

    private Coroutine flashRoutine;

    private void Start()
    {
        StartCoroutine(WaitForViewModel());
    }

    private System.Collections.IEnumerator WaitForViewModel()
    {
        while (viewModel == null)
        {
            viewModel = FindObjectOfType<PlayerViewModel>();
            yield return null;
        }

        viewModel.CurrentAmmo.ChangeEvent += OnAmmoChanged;
        viewModel.MagazineSize.ChangeEvent += OnAmmoChanged;
        viewModel.IsReloading.ChangeEvent += OnReloadStateChanged;
        viewModel.ReloadProgress.ChangeEvent += OnReloadProgressChanged;
    }

    private void OnDisable()
    {
        if (viewModel == null) return;

        viewModel.CurrentAmmo.ChangeEvent -= OnAmmoChanged;
        viewModel.MagazineSize.ChangeEvent -= OnAmmoChanged;
        viewModel.IsReloading.ChangeEvent -= OnReloadStateChanged;
        viewModel.ReloadProgress.ChangeEvent -= OnReloadProgressChanged;
    }

    void OnAmmoChanged(int _)
    {
        int current = viewModel.CurrentAmmo.Value;
        int max = viewModel.MagazineSize.Value;

        ammoText.text = $"{current}/{max}";

        if (max == 0) return;

        AmmoState state = GetAmmoState(current, max);
        ApplyAmmoState(state, current);
    }

    AmmoState GetAmmoState(int current, int max)
    {
        if (current == 0)
            return AmmoState.Empty;

        float percent = (float)current / max;

        bool isCritical = percent <= criticalPercent && current <= criticalMinBullets;

        if (isCritical)
            return AmmoState.Critical;

        if (percent <= lowPercent)
            return AmmoState.Low;

        return AmmoState.Normal;
    }

    void ApplyAmmoState(AmmoState state, int current)
    {
        if (flashRoutine != null && state != AmmoState.Critical)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
            ammoText.enabled = true;
        }

        switch (state)
        {
            case AmmoState.Normal:
                ammoText.color = normalColor;
                break;

            case AmmoState.Low:
                ammoText.color = lowColor;
                break;

            case AmmoState.Critical:
                ammoText.color = criticalColor;
                if (flashRoutine == null)
                    flashRoutine = StartCoroutine(FlashText());
                break;

            case AmmoState.Empty:
                if (flashRoutine != null)
                {
                    StopCoroutine(flashRoutine);
                    flashRoutine = null;
                    ammoText.enabled = true;
                }
                ammoText.color = criticalColor;
                break;
        }
    }

    void OnReloadStateChanged(bool isReloading)
    {
        if (!isReloading)
            OnAmmoChanged(0);
    }

    void OnReloadProgressChanged(float progress)
    {
        if (!viewModel.IsReloading.Value) return;

        int max = viewModel.MagazineSize.Value;
        int startAmmo = viewModel.CurrentAmmo.Value;

        int displayedAmmo = Mathf.RoundToInt(Mathf.Lerp(startAmmo, max, progress));
        ammoText.text = $"{displayedAmmo}/{max}";
        ammoText.color = Color.grey;
    }

    System.Collections.IEnumerator FlashText()
    {
        while (true)
        {
            int current = viewModel.CurrentAmmo.Value;
            int max = viewModel.MagazineSize.Value;

            if (GetAmmoState(current, max) != AmmoState.Critical)
                break;

            ammoText.enabled = !ammoText.enabled;
            yield return new WaitForSeconds(0.2f);
        }

        ammoText.enabled = true;
        flashRoutine = null;
    }

    enum AmmoState { Normal, Low, Critical, Empty }
}