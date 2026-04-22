using System;
using UnityEngine;

namespace Resonance.Assemblies.Train
{
    [Serializable]
    public struct TrainConfig
    {
        [Header("Movement")]
        public float maxSpeed;
        public float accelerationTime;
        public float decelerationTime;
        public float arrivalTolerance;

        [Header("Station Behaviour")]
        public float stationStopDuration;
        public float preDepartWarningTime;

        public float Acceleration => accelerationTime > 0f ? maxSpeed / accelerationTime : maxSpeed;
        public float Deceleration => decelerationTime > 0f ? maxSpeed / decelerationTime : maxSpeed;

        public static TrainConfig Default => new TrainConfig
        {
            maxSpeed = 14f,
            accelerationTime = 4f,
            decelerationTime = 3f,
            arrivalTolerance = 0.15f,
            stationStopDuration = 15f,
            preDepartWarningTime = 3f,
        };
    }
}
