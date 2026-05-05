using PurrNet.Prediction;

namespace Resonance.PlayerController
{
    public struct PlayerSkinRendererInputData : IPredictedData
    {
        public bool HasSkinRequest;
        public int SkinIndex;
        public readonly void Dispose() { }
    }
}
