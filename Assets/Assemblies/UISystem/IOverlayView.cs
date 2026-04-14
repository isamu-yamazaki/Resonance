namespace Resonance.Assemblies.UISystem
{
    public interface IOverlayView
    {
        public void OnShow(OverlayViewActions viewActions);
        public void OnHide();
    }
}
