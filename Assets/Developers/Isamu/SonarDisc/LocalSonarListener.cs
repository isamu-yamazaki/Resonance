using System.Collections;
using PurrNet;
using PurrNet.Prediction;
using Resonance.Helper;
using UnityEngine;

namespace Resonance.Abilities.SonarDisc
{
    /// <summary>
    /// Lives on the player prefab; only the locally-owned instance binds to the
    /// <see cref="SonarScanNetworkAdapter"/> singleton. The adapter's events are global to the singleton,
    /// so subscribing from every player instance would fire all of them on the receiving client — the
    /// <see cref="isOwner"/> gate keeps exactly one handler per client.
    /// </summary>
    public class LocalSonarListener : PredictedIdentity<LocalSonarListenerState>
    {
#if !UNITY_SERVER
        [Header("Wwise Events")]
        // Plays only for the disc owner on a successful scan (Play_SD_Ping). Moved here from SonarDiscProjectile.
        [SerializeField] private AK.Wwise.Event scanConfirmedEvent;

        // Plays for the victim when their own player is scanned (directional warning cue).
        [SerializeField] private AK.Wwise.Event scannedWarningEvent;
#endif

        // The exact instance we bound to, so we unsubscribe from the same object even if Instance changed.
        private SonarScanNetworkAdapter _adapter;

        // LateAwake runs once the object is fully set up (owner + predictionManager assigned), so isOwner is
        // valid here — same place PlayerPredictedController reads it. Runs on every peer, hence the gate.
        protected override void LateAwake()
        {
            if (!isOwner)
                return;

            StartCoroutine(BindWhenReady());
        }

        // Fires on both despawn and OnDestroy; Unbind is idempotent so double-invocation is harmless.
        protected override void Destroyed()
        {
            Unbind();
        }

        // The adapter sets Instance in its Awake, but spawn order between it and a player isn't guaranteed,
        // so wait it out (matches the Compass / LocalPlayer-wait idiom) rather than assuming it's ready.
        private IEnumerator BindWhenReady()
        {
            while (SonarScanNetworkAdapter.Instance == null)
                yield return null;

            _adapter = SonarScanNetworkAdapter.Instance;
            _adapter.OnLocalPlayerDetectedSomeone += HandleDetectedSomeone;
            _adapter.OnLocalPlayerWasScanned += HandleWasScanned;
        }

        private void Unbind()
        {
            if (_adapter == null)
                return;

            _adapter.OnLocalPlayerDetectedSomeone -= HandleDetectedSomeone;
            _adapter.OnLocalPlayerWasScanned -= HandleWasScanned;
            _adapter = null;
        }

        /// <summary>
        /// Owner's client: a pulse we own revealed <paramref name="detected"/>. Reveal their body through walls
        /// and play the owner-only confirmation ping. (Old SonarDiscProjectile.NotifyPlayerDetected + NotifyScanConfirmed.)
        /// </summary>
        private void HandleDetectedSomeone(PlayerID detected, Vector3 detectedPos)
        {
            GameObject detectedPlayer = OwnerFinder.FindPlayerGameObjectById(detected);
            if (detectedPlayer != null)
            {
                ScannedHighlight highlight = detectedPlayer.GetComponentInChildren<ScannedHighlight>();
                if (highlight != null)
                    highlight.Play();
            }

#if !UNITY_SERVER
            if (scanConfirmedEvent != null && scanConfirmedEvent.IsValid())
                scanConfirmedEvent.Post(gameObject);
#endif
        }

        /// <summary>
        /// Victim's client: our own player was scanned from <paramref name="sourcePos"/>. Plays a warning cue.
        /// </summary>
        private void HandleWasScanned(Vector3 sourcePos)
        {
#if !UNITY_SERVER
            if (scannedWarningEvent != null && scannedWarningEvent.IsValid())
                scannedWarningEvent.Post(gameObject);
#endif
            // TODO: drive a directional warning UI toward sourcePos (no component existed in the old impl).
        }
    }

    public struct LocalSonarListenerState : IPredictedData<LocalSonarListenerState>
    {
        public void Dispose()
        {
        }
    }
}
