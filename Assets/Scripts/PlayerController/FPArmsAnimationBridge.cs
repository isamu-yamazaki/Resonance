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

        public void OnUndrawComplete()
        {
            _driver?.OnUndrawComplete();
        }
    }
}