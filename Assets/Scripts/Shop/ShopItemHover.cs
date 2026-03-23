using UnityEngine;
using UnityEngine.EventSystems;
using Resonance.Combat.Weapons;

public class ShopItemHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private WeaponProperties weapon;
    private WeaponStatsDisplay statsDisplay;

    // Pass in the display directly
    public void Setup(WeaponProperties weapon, WeaponStatsDisplay display)
    {
        this.weapon = weapon;
        this.statsDisplay = display;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (weapon != null && statsDisplay != null)
        {
            statsDisplay.ShowStats(weapon);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (statsDisplay != null)
        {
            statsDisplay.Hide();
        }
    }
}