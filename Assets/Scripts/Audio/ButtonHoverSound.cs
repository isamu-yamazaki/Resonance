#if !UNITY_SERVER
using UnityEngine;
using UnityEngine.EventSystems;

namespace Resonance.Shop
{
    public class ButtonHoverSound : MonoBehaviour, IPointerEnterHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            AkSoundEngine.PostEvent("Play_UI_Button_Hover", gameObject);
        }
    }
}
#endif
