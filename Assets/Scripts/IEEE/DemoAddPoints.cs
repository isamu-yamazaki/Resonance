using System;
using PurrNet;
using Resonance.Match;
using UnityEngine;
using UnityEngine.InputSystem;

public class DemoAddPoints : NetworkBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    private InputAction addPointsAction;

    private void Awake()
    {
        InputActionMap map = inputActions.FindActionMap("DemoActionMap");
        addPointsAction = map.FindAction("AddPoints");


    }

    private void OnEnable()
    {
        addPointsAction.Enable();
    }

    private void OnDisable()
    {
        addPointsAction.Disable();
    }

    private void Update()
    {
        if (addPointsAction.WasPressedThisFrame())
            AddPointsForLocalPlayer();
    }

    private void AddPointsForLocalPlayer()
    {
        if (!localPlayer.HasValue)
            return;

        if (MatchLogicNetworkAdapter.Instance == null)
            return;

        var matchStats = MatchLogicNetworkAdapter.Instance.GetTemporaryMatchStatsReference();
        if (matchStats == null)
            return;

        matchStats.RecordDamage_Server(localPlayer.GetValueOrDefault().id.value, localPlayer.GetValueOrDefault().id.value, 100);
    }
}
