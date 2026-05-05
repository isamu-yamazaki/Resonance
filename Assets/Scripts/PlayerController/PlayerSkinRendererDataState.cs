using PurrNet.Prediction;

namespace Resonance.PlayerController
{
    public struct PlayerSkinRendererDataState : IPredictedData<PlayerSkinRendererDataState>
    {
        public int SkinIndex;
        public readonly void Dispose() { }
    }
}
