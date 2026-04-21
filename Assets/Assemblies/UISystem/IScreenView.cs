namespace Resonance.Assemblies.UISystem
{
    public interface IScreenView
    {
        string Key { get; }
        void OnShow(ScreenViewActions viewActions);
        void OnHide();
    }
}
