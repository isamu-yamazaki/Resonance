using UnityEngine;

namespace Resonance.Combat.Augments
{
    public class GrappleArmsAnimationBridge : MonoBehaviour
    {
        private FPArmsAnimator _driver;

        private void Awake()
        {
            _driver = GetComponentInParent<FPArmsAnimator>();
        }

        public void OnGrappleFireHook()
        {
            _driver?.OnGrappleFireHook();
        }

        public void OnGrappleComplete()
        {
            _driver?.OnGrappleComplete();
        }
    }
}