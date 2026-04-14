using UnityEngine.InputSystem;

namespace Resonance.Assemblies.UISystem
{
    public struct OverlayOptions
    {
        public IOverlayView view;
        public bool unlockCursorWhenShown;
        public InputActionMap[] inputMapsToDisableWhenShown;
    }
}
