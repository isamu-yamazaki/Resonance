using UnityEngine;

namespace Resonance.Combat.Weapons
{
    [CreateAssetMenu(fileName = "New Weapon Audio", menuName = "Resonance/Weapons/Weapon Audio Properties")]
    public class WeaponAudioProperties : ScriptableObject
    {
        [Header("Muzzle Events")]
        public AK.Wwise.Event fireEvent;
        public AK.Wwise.Event emptyTriggerEvent;

        [Header("Body Events")]
        public AK.Wwise.Event equipEvent;
        public AK.Wwise.Event reloadEvent;
    }
}
