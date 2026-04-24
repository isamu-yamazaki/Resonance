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
    [SerializeField] private float lowPercent = 0.5f;
    [SerializeField] private float criticalPercent = 0.25f;

    private int reloadStartAmmo;
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
        viewModel.MagazineSize.ChangeEvent += OnMagazineSizeChanged;
        viewModel.IsReloading.ChangeEvent += OnReloadStateChanged;
        viewModel.ReloadProgress.ChangeEvent += OnReloadProgressChanged;

        RefreshAmmo();
    }

    private void OnDisable()
    {
        if (viewModel == null) return;

        viewModel.CurrentAmmo.ChangeEvent -= OnAmmoChanged;
        viewModel.MagazineSize.ChangeEvent -= OnMagazineSizeChanged;
        viewModel.IsReloading.ChangeEvent -= OnReloadStateChanged;
        viewModel.ReloadProgress.ChangeEvent -= OnReloadProgressChanged;
    }

    private void OnAmmoChanged(int _) => RefreshAmmo();
    private void OnMagazineSizeChanged(int _) => RefreshAmmo();

    private void RefreshAmmo()
    {
        if (viewModel.IsReloading.Value) return;

        int current = viewModel.CurrentAmmo.Value;
        int max = viewModel.MagazineSize.Value;

        ammoText.text = $"{current}/{max}";

        if (max == 0) return;

        ApplyAmmoState(GetAmmoState(current, max));
    }

    private AmmoState GetAmmoState(int current, int max)
    {
        if (current == 0)
            return AmmoState.Critical; // red + flash at empty, always

        float percent = (float)current / max;

        if (current == 1 && max > 1)
            return AmmoState.Danger; // last bullet on a multi-round gun → red, no flash

        if (percent <= criticalPercent)
            return AmmoState.Danger; // red, no flash

        if (percent <= lowPercent)
            return AmmoState.Low; // orange

        return AmmoState.Normal;
    }

    private void ApplyAmmoState(AmmoState state)
    {
        if (state != AmmoState.Critical)
            StopFlash();

        switch (state)
        {
            case AmmoState.Normal:
                ammoText.color = normalColor;
                break;

            case AmmoState.Low:
                ammoText.color = lowColor;
                break;

            case AmmoState.Danger:
                ammoText.color = criticalColor;
                break;

            case AmmoState.Critical:
                ammoText.color = criticalColor;
                if (flashRoutine == null)
                    flashRoutine = StartCoroutine(FlashText());
                break;
        }
    }

    private void OnReloadStateChanged(bool isReloading)
    {
        if (isReloading)
        {
            reloadStartAmmo = viewModel.CurrentAmmo.Value;
            StopFlash();
        }
        else
        {
            RefreshAmmo();
        }
    }

    private void OnReloadProgressChanged(float progress)
    {
        if (!viewModel.IsReloading.Value) return;

        int max = viewModel.MagazineSize.Value;
        int displayedAmmo = Mathf.RoundToInt(Mathf.Lerp(reloadStartAmmo, max, progress));

        ammoText.text = $"{displayedAmmo}/{max}";
        ammoText.color = Color.grey;
    }

    private void StopFlash()
    {
        if (flashRoutine == null) return;

        StopCoroutine(flashRoutine);
        flashRoutine = null;
        ammoText.enabled = true;
    }

    private System.Collections.IEnumerator FlashText()
    {
        while (GetAmmoState(viewModel.CurrentAmmo.Value, viewModel.MagazineSize.Value) == AmmoState.Critical)
        {
            ammoText.enabled = !ammoText.enabled;
            yield return new WaitForSeconds(0.2f);
        }

        ammoText.enabled = true;
        flashRoutine = null;
    }

    private enum AmmoState { Normal, Low, Danger, Critical }
}