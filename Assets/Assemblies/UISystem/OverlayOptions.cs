using UnityEngine.InputSystem;

namespace Resonance.Assemblies.UISystem
{
    public struct OverlayOptions
    {
        public OverlayView view;
        public bool unlockCursorWhenShown;
        public InputActionMap[] inputMapsToDisableWhenShown;
    }
}
