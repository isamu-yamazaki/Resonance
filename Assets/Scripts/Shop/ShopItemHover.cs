using UnityEngine;
using UnityEngine.EventSystems;
using Resonance.Combat.Weapons;

public class ShopItemHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private WeaponProperties weapon;                 // The weapon this shop item represents
    private WeaponStatsDisplay statsDisplay;         // Reference to the stats panel

    public void Setup(WeaponProperties weapon)
    {
        this.weapon = weapon;

        // Find the panel in the scene at runtime
        if (statsDisplay == null)
        {
            statsDisplay = FindObjectOfType<WeaponStatsDisplay>();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (weapon != null && statsDisplay != null)
        {
            statsDisplay.ShowStats(weapon);  // Show the panel
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (statsDisplay != null)
        {
            statsDisplay.Hide();             // Hide the panel
        }
    }
}