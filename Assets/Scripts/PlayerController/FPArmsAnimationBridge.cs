using UnityEngine;

namespace Resonance.Combat
{
    public class FPArmsAnimationBridge : MonoBehaviour
    {
        private FPArmsAnimator _driver;

        private void Awake()
        {
            _driver = GetComponentInParent<FPArmsAnimator>();
        }

        public void OnHolsterComplete()
        {
            _driver?.OnHolsterComplete();
        }

        public void OnDrawComplete()
        {
            _driver?.OnDrawComplete();
        }
    }
}