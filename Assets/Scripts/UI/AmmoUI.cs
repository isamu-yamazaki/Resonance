using UnityEngine;
using TMPro;

public class AmmoUI : MonoBehaviour
{
    private PlayerViewModel viewModel;

    [SerializeField] private TextMeshProUGUI ammoText;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color halfColor = new Color(1f, 0.6f, 0f);
    [SerializeField] private Color lowColor = Color.red;

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

        float percent = (float)current / max;

        if (percent <= 0.1f)
        {
            ammoText.color = lowColor;

            if (flashRoutine == null)
                flashRoutine = StartCoroutine(FlashText());
        }
        else
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
                ammoText.enabled = true;
            }

            ammoText.color = percent <= 0.5f ? halfColor : normalColor;
        }
    }

    void OnReloadStateChanged(bool isReloading)
    {
        if (!isReloading)
            ammoText.color = normalColor;
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
        while (viewModel.CurrentAmmo.Value > 0 &&
               (float)viewModel.CurrentAmmo.Value / viewModel.MagazineSize.Value <= 0.1f)
        {
            ammoText.enabled = !ammoText.enabled;
            yield return new WaitForSeconds(0.2f);
        }

        ammoText.enabled = true;
        flashRoutine = null;
    }
}
