using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Resonance.PlayerController;

namespace Resonance.UI
{
    public class DamageIndicatorUI : MonoBehaviour
    {
        public static DamageIndicatorUI Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private DamageIndicatorArc arcPrefab;
        [SerializeField] private int maxArcs = 3;
        [SerializeField] private float mergeAngleThreshold = 30f;

        private List<DamageIndicatorArc> _arcs = new List<DamageIndicatorArc>();
        private Camera _camera;

        #region Startup

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private IEnumerator Start()
        {
            while (PlayerController.PlayerController.LocalPlayer == null)
                yield return null;

            _camera = Camera.main;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region Public Methods

        public void ShowIndicator(Vector3 attackerWorldPosition)
        {
            if (_camera == null) return;

            float angle = CalculateAngle(attackerWorldPosition);

            DamageIndicatorArc existing = FindArcInRange(angle);

            if (existing != null)
            {
                existing.Refresh(angle);
                return;
            }

            DamageIndicatorArc arc = GetArc();
            arc.Activate(angle);
        }

        #endregion

        #region Helpers

        private float CalculateAngle(Vector3 attackerWorldPosition)
        {
            Vector3 directionToAttacker = attackerWorldPosition - _camera.transform.position;
            directionToAttacker.y = 0f;

            Vector3 cameraForward = _camera.transform.forward;
            cameraForward.y = 0f;

            float angle = Vector3.SignedAngle(cameraForward, directionToAttacker, Vector3.up);
            return angle;
        }

        private DamageIndicatorArc FindArcInRange(float angle)
        {
            foreach (DamageIndicatorArc arc in _arcs)
            {
                if (!arc.IsActive) continue;

                float delta = Mathf.Abs(Mathf.DeltaAngle(arc.Angle, angle));
                if (delta <= mergeAngleThreshold)
                    return arc;
            }

            return null;
        }

        private DamageIndicatorArc GetArc()
        {
            foreach (DamageIndicatorArc arc in _arcs)
            {
                if (!arc.IsActive)
                    return arc;
            }

            if (_arcs.Count < maxArcs)
            {
                DamageIndicatorArc newArc = Instantiate(arcPrefab, transform);
                _arcs.Add(newArc);
                return newArc;
            }

            // Pool full — replace the oldest (first active found)
            return _arcs[0];
        }

        #endregion
    }
}
