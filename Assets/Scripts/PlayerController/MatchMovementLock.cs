using PurrNet;
using Resonance.Assemblies.SharedGameLogic;
using Resonance.Match;
using UnityEngine;

namespace Resonance.PlayerController
{
    public class MatchMovementLock : NetworkBehaviour
    {
        private BaseRoundManagerNetworkAdapter roundManager;
        private PlayerState playerState;

        protected override void OnSpawned()
        {
            base.OnSpawned();
            enabled = isOwner;
        }

        private void Awake()
        {
            roundManager = MatchLogicNetworkAdapter.Instance?.ActiveRoundManager;
            playerState = GetComponent<PlayerState>();
        }

        private void Update()
        {
            if (roundManager == null) return;

            if (roundManager.IsMatchEnded)
            {
                playerState.SetPlayerMovementState(PlayerMovementState.MatchEndedFrozen);
            } else if (!roundManager.IsMatchActive)
            {
                playerState.SetPlayerMovementState(PlayerMovementState.PreMatchFrozen);
            } else if (playerState.IsMatchFrozen())
            {
                playerState.SetPlayerMovementState(PlayerMovementState.Idling);
            }
        }

    }
}
