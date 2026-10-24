using PurrNet.Prediction;
using Resonance.Audio;
using Resonance.Helper.PredictedAudioBroadcast;
using UnityEngine;

namespace Resonance.PlayerController
{
    public class
        OverdriveWorldActivateBroadcast : PredictedIdentity<PredictedAudioBroadcastState>
    {
#if !UNITY_SERVER
        [SerializeField] private AK.Wwise.Event activateWorldEvent;
#endif

        private bool _requestAudioBroadcast;

        protected override void Simulate(ref PredictedAudioBroadcastState state,
            float delta)
        {
            if (state.BroadcastAudioNextTick)
            {
                state.BroadcastAudio = true;
                state.BroadcastAudioNextTick = false;
            }
            else
            {
                state.BroadcastAudio = false;
            }
        }

        public void SimulateBroadcastAudio()
        {
            currentState.BroadcastAudioNextTick = true;
        }

        protected override void UpdateView(PredictedAudioBroadcastState viewState,
            PredictedAudioBroadcastState? verified)
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
            _requestAudioBroadcast = true;
        }
    }
}