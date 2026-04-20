namespace Resonance.Assemblies.UISystem
{
    public interface IScreenView
    {
        public void OnShow(ScreenViewActions viewActions);
        public void OnHide();
    }
}
