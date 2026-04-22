using UnityEngine;

namespace Resonance.Assemblies.Train
{
    public readonly struct TrainStationData
    {
        public readonly Vector3 stopPosition;
        public readonly string displayName;

        public TrainStationData(Vector3 stopPosition, string displayName)
        {
            this.stopPosition = stopPosition;
            this.displayName = displayName;
        }
    }
}
