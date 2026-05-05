using PurrNet.Prediction;

namespace Resonance.Combat
{
    public struct PlayerShooterInputData : IPredictedData
    {
        public bool AttackPressed { get; set; }
        public bool AttackHeld { get; set; }
        public bool ReloadPressed { get; set; }

        public readonly void Dispose() { }
    }
}
