using System;
using PurrNet.Prediction;

namespace Resonance.PlayerController
{
    public struct OverdriveTrailEffectState : IPredictedData<OverdriveTrailEffectState>
    {
        public bool SpawnGhosts;
        public DateTime GhostSpawningStartTime;

        public readonly void Dispose()
        {
        }
    }
}
