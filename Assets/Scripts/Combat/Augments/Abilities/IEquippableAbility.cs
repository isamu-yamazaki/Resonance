namespace Resonance.Combat.Augments
{
    /// <summary>
    /// Implemented by abilities that need an explicit "equipped" gate instead of relying on Unity's
    /// <c>enabled</c> flag.
    ///
    /// Toggling a PurrNet <c>PredictedIdentity</c> via <c>MonoBehaviour.enabled</c> is unsafe: the
    /// prediction loop ignores <c>enabled</c> (it keeps simulating and serializing input) while Unity
    /// suppresses the <c>Update()</c> that feeds <c>UpdateInput</c>, which stops input from being
    /// transmitted to the server correctly. Predicted abilities therefore stay permanently enabled and
    /// track equipped-ness in predicted state; <see cref="PlayerAbilityManager"/> drives that through
    /// <see cref="SetEquipped"/> on equip/unequip.
    /// </summary>
    public interface IEquippableAbility
    {
        /// <summary>
        /// Set equipped-ness. For predicted abilities this mutates predicted state and must only be
        /// called from within the simulation loop (e.g. <c>PlayerAbilityManager.Simulate</c>); for
        /// non-predicted abilities it mutates a plain field.
        /// </summary>
        void SetEquipped(bool equipped);
    }
}
