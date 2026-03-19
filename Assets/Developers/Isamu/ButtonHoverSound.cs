using UnityEngine;
using UnityEngine.EventSystems;

namespace Resonance.Shop
{
    public class ButtonHoverSound : MonoBehaviour, IPointerEnterHandler
    {
        [SerializeField] private AK.Wwise.Event buttonHoverEvent;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (buttonHoverEvent != null && buttonHoverEvent.IsValid())
                buttonHoverEvent.Post(gameObject);
        }
    }
}
