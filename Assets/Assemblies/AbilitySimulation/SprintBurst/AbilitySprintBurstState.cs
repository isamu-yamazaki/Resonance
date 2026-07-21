using PurrNet.Prediction;

namespace Resonance.Assemblies.AbilitySimulation.SprintBurst
{
    public struct AbilitySprintBurstState : IPredictedData<AbilitySprintBurstState>
    {
        /// <summary>
        /// Equipped gate, set by the owning behaviour (via PlayerAbilityManager). Not read by Step —
        /// the MonoBehaviour gates simulation/input on it.
        /// </summary>
        public bool IsEquipped;

        public float TimeSinceLastSprinting;
        public float CurrentMeter;
        public bool WasSprinting;

        /// <summary>
        /// The multiplicative speed modifier this ability wants applied this tick (0 = none). Step only
        /// computes it; the owning AbilitySprintBurst behaviour reconciles PlayerStats against it.
        /// </summary>
        public float LastAppliedSpeedMod;

        public void Dispose() { }
    }
}
