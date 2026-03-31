using UnityEngine;
using UnityEngine.EventSystems;

namespace Resonance.Shop
{
    public class InventoryItemHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private GameObject sellButton;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (sellButton != null)
                sellButton.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (sellButton != null)
                sellButton.SetActive(false);
        }
    }
}