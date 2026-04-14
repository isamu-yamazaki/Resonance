using UnityEngine.InputSystem;

namespace Resonance.Assemblies.UISystem
{
    public struct OverlayOptions
    {
        public IOverlayView view;
        public bool unlockCursorWhenShown;
        private InputActionMap[] _inputMapsToDisableWhenShown;
        public InputActionMap[] inputMapsToDisableWhenShown
        {
            get => _inputMapsToDisableWhenShown ?? System.Array.Empty<InputActionMap>();
            set => _inputMapsToDisableWhenShown = value;
        }
    }
}
