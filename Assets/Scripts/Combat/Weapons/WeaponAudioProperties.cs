using UnityEngine;

namespace Resonance.Combat.Weapons
{
    [CreateAssetMenu(fileName = "New Weapon Audio", menuName = "Resonance/Weapons/Weapon Audio Properties")]
    public class WeaponAudioProperties : ScriptableObject
    {
        public string key;

#if !UNITY_SERVER
        [Header("Muzzle Events")]
        public AK.Wwise.Event fireEvent;
        public AK.Wwise.Event emptyTriggerEvent;
        public AK.Wwise.Event casingEvent;

        [Header("Body Events")]
        public AK.Wwise.Event equipEvent;
        public AK.Wwise.Event reloadEvent;
#endif
    }
}
