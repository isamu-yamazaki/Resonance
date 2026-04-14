using UnityEngine;

namespace Resonance.Assemblies.UISystem
{
    public abstract class OverlayView : MonoBehaviour
    {
        public void Awake()
        {
            // All overlay views start out disabled
            enabled = false;
        }

        public virtual void Show()
        {
            enabled = true;
        }

        public virtual void Hide()
        {
            enabled = false;
        }
    }
}
