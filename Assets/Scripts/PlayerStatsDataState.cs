using PurrNet.Pooling;
using PurrNet.Prediction;
using UnityEngine;

namespace Resonance.Player
{
    public struct PlayerStatsDataState : IPredictedData<PlayerStatsDataState>
    {
        public float CurrentHealth;
        public bool IsDead;
        public float CurrentSpeed;
        public float CurrentDamageReduction;
        public float CurrentHealthRegen;
        public float RespawnTimer;
        public Vector3 LastDamageAttackerPos;
        public Vector3 SpawnPosition;
        public Quaternion SpawnRotation;
        public DisposableList<float> SpeedModifiers;

        public readonly void Dispose() { }
    }
}
