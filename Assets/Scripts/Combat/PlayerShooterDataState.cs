using PurrNet.Prediction;
using UnityEngine;

namespace Resonance.Combat
{
    public struct PlayerShooterDataState : IPredictedData<PlayerShooterDataState>
    {
        public int AmmoSlot0;
        public int AmmoSlot1;
        public float FireCooldown;
        public float ReloadTimer;
        public bool IsEmptyReload;
        public float CurrentSpread;
        public int LastEquippedSlot;
        public int ShotCount;
        public int ReloadStartCount;
        public int ReloadEndCount;
        public int EmptyTriggerCount;
        public Vector3 LastShotEndPoint;
        public bool LastShotHitPlayer;
        public float LastShotDamage;

        public readonly void Dispose() { }
    }
}
