using Resonance.Match;
using UnityEngine;

namespace Resonance.PlayerController
{
    public class MatchMovementLock : MonoBehaviour
    {
        private BaseRoundManagerNetworkAdapter roundManager;
        private PlayerState playerState;
        private void Awake()
        {
            roundManager = MatchLogicNetworkAdapter.Instance?.ActiveRoundManager;
            playerState = GetComponent<PlayerState>();
        }

        private void Update()
        {
            if (!roundManager.IsMatchActive)
            {
                playerState.SetPlayerMovementState(PlayerMovementState.MatchFrozen);
            } else if (playerState.IsMatchFrozen())
            {
                playerState.SetPlayerMovementState(PlayerMovementState.Idling);
            }
        }

    }
}
