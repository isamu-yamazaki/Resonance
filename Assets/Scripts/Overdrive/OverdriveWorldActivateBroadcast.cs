using PurrNet.Prediction;
using Resonance.Audio;
using UnityEngine;

namespace Resonance.PlayerController
{
    public class OverdriveWorldActivateBroadcast : PredictedIdentity<OverdriveWorldActivateBroadcastInput, OverdriveWorldActivateBroadcastState>
    {

#if !UNITY_SERVER
        [SerializeField] private AK.Wwise.Event activateWorldEvent;
#endif

        private bool requestAudioBroadcast;

        protected override void GetFinalInput(ref OverdriveWorldActivateBroadcastInput input)
        {
            input.RequestAudioBroadcastNextTick = requestAudioBroadcast;
            requestAudioBroadcast = false;
        }

        protected override void Simulate(OverdriveWorldActivateBroadcastInput input, ref OverdriveWorldActivateBroadcastState state, float delta)
        {
            state.BroadcastAudio = input.RequestAudioBroadcastNextTick;
        }

        protected override void UpdateView(OverdriveWorldActivateBroadcastState viewState, OverdriveWorldActivateBroadcastState? verified)
        {
            if (!verified.HasValue) return;
            var v = verified.Value;

            if (v.BroadcastAudio)
            {
                Debug.Log("[OverdriveAudioBroadcast] Playing sound effect");

#if !UNITY_SERVER
                if (activateWorldEvent != null && activateWorldEvent.IsValid())
                    activateWorldEvent.Post(gameObject);

                if (AudioSourceTracker.Instance != null)
                    AudioSourceTracker.Instance.RegisterSound(transform.position, 1f);
#endif
            }
        }

        public void RequestAudioBroadcastNextTick()
        {
            requestAudioBroadcast = true;
        }
    }
}
