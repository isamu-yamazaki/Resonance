using System;
using Resonance.Combat.Augments;
using Resonance.Combat.Mods;
using Resonance.Combat.Weapons;
using Resonance.Combat.Weapons.Enums;
using Resonance.Inventory;
using Resonance.Shop;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public struct Loadout
{
    public WeaponProperties Primary;
    public WeaponProperties Secondary;
    public AugmentProperties Upper;
    public AugmentProperties Lower;
    public WeaponModProperties[] PrimaryMods;
    public WeaponModProperties[] SecondaryMods;
}
public class DemoPresetLoadouts : MonoBehaviour
{
    public Loadout[] Loadouts;
    private ShopOverlayView shop;

    [SerializeField] private InputActionAsset inputActions;
    private InputAction[] _loadoutActions;

    private void Awake()
    {
        InputActionMap map = inputActions.FindActionMap("DemoActionMap");
        _loadoutActions = new InputAction[]
        {
            map.FindAction("Load1"),
            map.FindAction("Load2"),
            map.FindAction("Load3"),
        };
    }

    private void Start()
    {
        shop = GetComponent<ShopOverlayView>();
    }

    private void OnEnable()
    {
        foreach (var action in _loadoutActions)
            action.Enable();
    }

    private void OnDisable()
    {
        foreach (var action in _loadoutActions)
            action.Disable();
    }

    private void Update()
    {
        for (int i = 0; i < _loadoutActions.Length; i++)
        {
            if (_loadoutActions[i].WasPressedThisFrame())
                ApplyLoadout(i);
        }
    }

    private void ApplyLoadout(int index)
    {
        if (index < 0 || index >= Loadouts.Length) return;
        Loadout loadout = Loadouts[index];

        if (loadout.Primary != null) shop.Buy(loadout.Primary);
        if (loadout.Secondary != null) shop.Buy(loadout.Secondary);
        if (loadout.Upper != null) shop.Buy(loadout.Upper);
        if (loadout.Lower != null) shop.Buy(loadout.Lower);

        if (loadout.PrimaryMods != null)
        {
            shop.SetModWeaponSlot(WeaponSlot.Primary);
            foreach (var mod in loadout.PrimaryMods)
                if (mod != null) shop.Buy(mod);
        }

        if (loadout.SecondaryMods != null)
        {
            shop.SetModWeaponSlot(WeaponSlot.Secondary);
            foreach (var mod in loadout.SecondaryMods)
                if (mod != null) shop.Buy(mod);
        }
    }
}
