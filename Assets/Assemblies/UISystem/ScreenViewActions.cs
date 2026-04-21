using System;

namespace Resonance.Assemblies.UISystem
{
    public struct ScreenViewActions
    {
        public Action Back;
        public Action<string> ShowScreen;
        public Action<string> ShowOverlay;
    }
}
