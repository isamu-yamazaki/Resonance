using PurrNet.Prediction;
using Resonance.Audio;
using Resonance.Helper.PredictedAudioBroadcast;
using UnityEngine;

namespace Resonance.Combat.Augments
{
    public class AbilityGrappleHookAudioBroadcast : PredictedIdentity<AbilityGrappleHookAudioBroadcastInput,
        AbilityGrappleHookAudioBroadcastState>
    {
        private bool _requestBroadcastGrappleRegistration;
        private bool _requestBroadcastShootAndTravel;
        private bool _requestBroadcastStopTravel;
        private bool _requestBroadcastRelease;
        private Vector3 _grappleRegistrationPosition;

#if !UNITY_SERVER
        [Header("Wwise Events")] [SerializeField]
        private AK.Wwise.Event shootEvent;

        [SerializeField] private AK.Wwise.Event travelLoopEvent;
        [SerializeField] private AK.Wwise.Event stopTravelEvent;
        [SerializeField] private AK.Wwise.Event releaseEvent;
#endif

        protected override void GetFinalInput(ref AbilityGrappleHookAudioBroadcastInput input)
        {
            input.RequestBroadcastGrappleRegistrationNextTick = _requestBroadcastGrappleRegistration;
            input.RequestBroadcastReleaseNextTick = _requestBroadcastRelease;
            input.RequestBroadcastShootAndTravelNextTick = _requestBroadcastShootAndTravel;
            input.RequestBroadcastStopTravelNextTick = _requestBroadcastStopTravel;
            input.GrappleRegistrationPosition = _grappleRegistrationPosition;

            _requestBroadcastGrappleRegistration = false;
            _requestBroadcastRelease = false;
            _requestBroadcastShootAndTravel = false;
            _requestBroadcastStopTravel = false;
        }

        protected override void Simulate(AbilityGrappleHookAudioBroadcastInput input,
            ref AbilityGrappleHookAudioBroadcastState state,
            float delta)
        {
            state.BroadcastGrappleRegistration = input.RequestBroadcastGrappleRegistrationNextTick;
            state.BroadcastShootAndTravel = input.RequestBroadcastShootAndTravelNextTick;
            state.BroadcastStopTravel = input.RequestBroadcastStopTravelNextTick;
            state.BroadcastRelease = input.RequestBroadcastReleaseNextTick;

            state.GrappleRegistrationPosition = input.GrappleRegistrationPosition;
        }

        protected override void UpdateView(AbilityGrappleHookAudioBroadcastState viewState,
            AbilityGrappleHookAudioBroadcastState? verified)
        {
            if (!verified.HasValue) return;
            var v = verified.Value;

#if !UNITY_SERVER
            if (v.BroadcastShootAndTravel)
            {
                if (shootEvent != null && shootEvent.IsValid())
                    shootEvent.Post(gameObject);

                if (travelLoopEvent != null && travelLoopEvent.IsValid())
                    travelLoopEvent.Post(gameObject);
            }

            if (v.BroadcastGrappleRegistration)
            {
                if (AudioSourceTracker.Instance != null)
                    AudioSourceTracker.Instance.RegisterSound(v.GrappleRegistrationPosition, 1f);
            }

            if (v.BroadcastStopTravel)
            {
                if (stopTravelEvent != null && stopTravelEvent.IsValid())
                    stopTravelEvent.Post(gameObject);
            }

            if (v.BroadcastRelease)
            {
                if (releaseEvent != null && releaseEvent.IsValid())
                    releaseEvent.Post(gameObject);
            }
#endif
        }

        #region Audio broadcast requests

        public void RequestExternalBroadcastGrappleRegistration(Vector3 position)
        {
            _grappleRegistrationPosition = position;
            _requestBroadcastGrappleRegistration = true;
        }

        public void RequestExternalBroadcastShootAndTravel()
        {
            _requestBroadcastShootAndTravel = true;
        }

        public void RequestExternalBroadcastStopTravel()
        {
            _requestBroadcastStopTravel = true;
        }

        public void RequestExternalBroadcastRelease()
        {
            _requestBroadcastRelease = true;
        }

        #endregion
    }

    public struct AbilityGrappleHookAudioBroadcastState : IPredictedData<AbilityGrappleHookAudioBroadcastState>
    {
        public bool BroadcastGrappleRegistration;
        public bool BroadcastShootAndTravel;
        public bool BroadcastStopTravel;
        public bool BroadcastRelease;

        public Vector3 GrappleRegistrationPosition;

        public void Dispose()
        {
        }
    }

    public struct AbilityGrappleHookAudioBroadcastInput : IPredictedData
    {
        // future: if grapple hook becomes predicted, set directly via simulation

        public bool RequestBroadcastGrappleRegistrationNextTick;
        public bool RequestBroadcastShootAndTravelNextTick;
        public bool RequestBroadcastStopTravelNextTick;
        public bool RequestBroadcastReleaseNextTick;

        public Vector3 GrappleRegistrationPosition;

        public void Dispose()
        {
        }
    }
}
