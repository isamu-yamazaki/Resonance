using PurrNet.Prediction;

namespace Resonance.PlayerController
{
    public struct PlayerSkinRendererDataState : IPredictedData<PlayerSkinRendererDataState>
    {
        public int SkinIndex;
        public int LastSkinIndex;
        public readonly void Dispose() { }
    }
}
