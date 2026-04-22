namespace Resonance.Assemblies.UISystem
{
    public interface IOverlayView
    {
        string Key { get; }
        void OnShow(OverlayViewActions viewActions);
        void OnHide();
    }
}
