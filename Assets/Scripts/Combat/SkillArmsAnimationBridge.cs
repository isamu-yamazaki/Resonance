using UnityEngine;

namespace Resonance.Combat
{
    public class SkillArmsAnimationBridge : MonoBehaviour
    {
        private FPArmsAnimator _driver;
        [SerializeField] private SkinnedMeshRenderer syringeRenderer;

        private void Awake()
        {
            _driver = GetComponentInParent<FPArmsAnimator>();
        }

        public void OnOverdriveAnimActivate()
        {
            _driver?.OnOverdriveAnimActivate();
        }

        public void OnStimAnimActivate()
        {
            _driver?.OnStimAnimActivate();
        }

        public void OnSkillComplete()
        {
            _driver?.OnSkillComplete();
        }
        
        public void ShowSyringe()
        {
            Debug.Log($"[SkillArmsAnimationBridge] ShowSyringe called - renderer: {syringeRenderer?.name ?? "NULL"}");
            if (syringeRenderer != null)
                syringeRenderer.enabled = true;
        }   

        public void HideSyringe()
        {
            if (syringeRenderer != null)
                syringeRenderer.enabled = false;
        }
    }
}