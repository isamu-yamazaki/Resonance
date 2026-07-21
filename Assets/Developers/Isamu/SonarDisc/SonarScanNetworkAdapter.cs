using System;
using PurrNet;
using UnityEngine;

namespace Resonance.Abilities.SonarDisc
{
    /// <summary>
    /// Singleton NetworkBehaviour that delivers sonar-scan results to specific players via TargetRpc.
    /// </summary>
    public class SonarScanNetworkAdapter : NetworkBehaviour
    {
        public static SonarScanNetworkAdapter Instance { get; private set; }

        /// <summary>
        /// Fired on the disc owner's client for each player their pulse detected (victim id + world position).
        /// Subscribe from the owner's view layer to drive the through-wall highlight.
        /// </summary>
        public event Action<PlayerID, Vector3> OnLocalPlayerDetectedSomeone;

        /// <summary>
        /// Fired on a client when their own player was scanned (carries the pulse source position for a
        /// directional warning). Carries no information about any other scanned player.
        /// </summary>
        public event Action<Vector3> OnLocalPlayerWasScanned;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        protected override void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            base.OnDestroy();
        }

        #region Server entry points

        /// <summary>Server-only: tell the disc owner that they detected <paramref name="detected"/>.</summary>
        [ServerOnly]
        public void NotifyOwnerOfDetection(PlayerID owner, PlayerID detected, Vector3 detectedPos)
        {
            NotifyOwnerOfDetectionRpc(owner, detected, detectedPos);
        }

        /// <summary>Server-only: tell <paramref name="victim"/> they were scanned. No other victim's data is included.</summary>
        [ServerOnly]
        public void NotifyScannedSelf(PlayerID victim, Vector3 sourcePos)
        {
            NotifyScannedSelfRpc(victim, sourcePos);
        }

        #endregion

        #region Targeted delivery

        [TargetRpc]
        private void NotifyOwnerOfDetectionRpc(PlayerID player, PlayerID detected, Vector3 detectedPos)
        {
            OnLocalPlayerDetectedSomeone?.Invoke(detected, detectedPos);
        }

        [TargetRpc]
        private void NotifyScannedSelfRpc(PlayerID player, Vector3 sourcePos)
        {
            OnLocalPlayerWasScanned?.Invoke(sourcePos);
        }

        #endregion
    }
}
