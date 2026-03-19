using System.Collections.Generic;
using Resonance.Combat.Weapons;
using Resonance.Combat.Weapons.Enums;
using TMPro;
using UnityEngine;

public class WeaponStatsDisplay : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject statLinePrefab;
    [SerializeField] private Transform contentParent;

    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private TextMeshProUGUI weaponClassText;
    
    private readonly List<GameObject> statLines = new List<GameObject>();

    public void ShowStats(WeaponProperties weapon)
    {
        Clear();
        panel.SetActive(true);
        
        weaponNameText.text = weapon.WeaponName;
        weaponClassText.text = FormatWeaponClass(weapon.Class);

        AddStat("Damage", weapon.Damage);
        AddStat("Fire Rate", weapon.FireRate);
        AddStat("Projectiles", weapon.ProjectilesPerShot);
        AddStat("Range", weapon.Range);
        AddStat("Accuracy", weapon.Accuracy);
        AddStat("Control", weapon.Control);
        AddStat("Mobility", weapon.Mobility);
        AddStat("Handling", weapon.Handling);
        AddStat("Magazine Size", weapon.MagazineSize);
        AddStat("Reload Time", weapon.ReloadTime);
        AddStat("Spread", weapon.Spread);
        AddStat("Muzzle Velocity", weapon.MuzzleVelocity);
        AddStat("Spread per Shot", weapon.SpreadPerShot);
        AddStat("Max Spread", weapon.MaxSpread);
        AddStat("Spread Recovery Rate", weapon.SpreadRecoveryRate);
    }

    public void Hide()
    {
        Clear();
        panel.SetActive(false);

        if (weaponNameText != null) weaponNameText.text = "";
        if (weaponClassText != null) weaponClassText.text = "";
    }

    private void AddStat(string name, float value)
    {
        GameObject go = Instantiate(statLinePrefab, contentParent);

        // 🔥 Explicit references instead of array guessing
        Transform nameTransform = go.transform.Find("StatName");
        Transform valueTransform = go.transform.Find("StatValue");

        if (nameTransform == null || valueTransform == null)
        {
            Debug.LogError("StatLinePrefab is missing StatName or StatValue!");
            return;
        }

        TextMeshProUGUI nameText = nameTransform.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI valueText = valueTransform.GetComponent<TextMeshProUGUI>();

        nameText.text = name;
        valueText.text = FormatValue(value);

        statLines.Add(go);
    }

    // ✨ Cleaner formatting (no unnecessary decimals)
    private string FormatValue(float value)
    {
        if (Mathf.Approximately(value % 1, 0))
        {
            return value.ToString("F0"); // whole number
        }
        return value.ToString("F1"); // decimal
    }
    
    private string FormatWeaponClass(WeaponClass weaponClass)
    {
        // Convert enum to clean string
        string text = weaponClass.ToString();

        // Optional: make it look like Valorant (ALL CAPS)
        return text.ToUpper();
    }

    private void Clear()
    {
        foreach (var go in statLines)
        {
            Destroy(go);
        }
        statLines.Clear();
    }
}