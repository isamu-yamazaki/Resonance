using UnityEngine;
using Resonance.Helper;

public class PlayerViewModel : MonoBehaviour
{
    // -----------------
    // HEALTH
    // -----------------
    public ObservableValue<float> Health { get; private set; }
    public float MaxHealth { get; private set; } = 100f;

    // -----------------
    // AMMO
    // -----------------
    public ObservableValue<int> CurrentAmmo { get; private set; }
    public ObservableValue<int> MagazineSize { get; private set; }
    public ObservableValue<bool> IsReloading { get; private set; }
    public ObservableValue<float> ReloadProgress { get; private set; } // 0 → 1

    void Awake()
    {
        Health = new ObservableValue<float>(MaxHealth);

        CurrentAmmo = new ObservableValue<int>(0);
        MagazineSize = new ObservableValue<int>(0);
        IsReloading = new ObservableValue<bool>(false);
        ReloadProgress = new ObservableValue<float>(0f);
    }

    public void InitializeHealth(float maxHealth)
    {
        MaxHealth = maxHealth;
        Health.Value = MaxHealth;
    }

    public void InitializeAmmo(int magazineSize)
    {
        MagazineSize.Value = magazineSize;
        CurrentAmmo.Value = magazineSize;
    }
    
    // Called by Shooter
    public void SetAmmo(int current, int max)
    {
        CurrentAmmo.Value = current;
        MagazineSize.Value = max;
    }

    public void SetReloadState(bool isReloading)
    {
        IsReloading.Value = isReloading;

        if (!isReloading)
            ReloadProgress.Value = 0f;
    }

    public void SetReloadProgress(float progress)
    {
        ReloadProgress.Value = progress;
    }

    public void TakeDamage(float amount)
    {
        Health.Value = Mathf.Max(Health.Value - amount, 0f);
    }

    public void Heal(float amount)
    {
        Health.Value = Mathf.Min(Health.Value + amount, MaxHealth);
    }
}