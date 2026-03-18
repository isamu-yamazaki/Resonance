using System.Collections.Generic;
using Resonance.Combat.Weapons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponStatsDisplay : MonoBehaviour
{
    [SerializeField] private GameObject panel; // your stats panel
    [SerializeField] private GameObject statLinePrefab; // prefab with two TextMeshProUGUI components
    [SerializeField] private Transform contentParent; // where stat lines go

    private readonly List<GameObject> statLines = new List<GameObject>();

    public void ShowStats(WeaponProperties weapon)
    {
        Clear();

        panel.SetActive(true);

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
    }

    private void AddStat(string name, float value)
    {
        GameObject go = Instantiate(statLinePrefab, contentParent);
        TextMeshProUGUI[] texts = go.GetComponentsInChildren<TextMeshProUGUI>();
        texts[0].text = name;
        texts[1].text = value.ToString("F1"); // format to 1 decimal
        statLines.Add(go);
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