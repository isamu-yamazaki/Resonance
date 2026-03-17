using PurrNet.Packing;
using UnityEngine;

namespace Resonance.Audio
{
    public class AudioSourceData: IPackedAuto
    {
        public Vector3 Position;
        public float Timestamp;
        public float Duration;
        public float PeakIntensity;

        public AudioSourceData(Vector3 position, float duration, float peakIntensity)
        {
            Position = position;
            Timestamp = Time.time;
            Duration = duration;
            PeakIntensity = peakIntensity;
        }

        public float GetAge()
        {
            return Time.time - Timestamp;
        }

        public bool IsExpired()
        {
            return GetAge() > Duration;
        }

        public float GetCurrentIntensity()
        {
            float age = GetAge();
            float normalizedAge = age / Duration;
            float fade = Mathf.Exp(-1.5f * normalizedAge);
            return PeakIntensity * fade;
        }
    }
}
