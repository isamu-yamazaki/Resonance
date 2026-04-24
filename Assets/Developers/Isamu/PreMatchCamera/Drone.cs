using System.Collections;
using UnityEngine;

namespace Resonance
{
    public class Drone : MonoBehaviour
    {
        [Header("Fly Away")]
        public float acceleration = 12f;
        public float maxSpeed = 22f;

        [Header("Orientation")]
        [Tooltip("Rotational offset applied on top of the spline node's forward. " +
                 "Adjust if the drone model's local forward doesn't match its mesh's nose direction.")]
        public Vector3 rotationOffset = Vector3.zero;

        [Tooltip("Local-space offset from the follow target. Use Z to push the drone behind the camera.")]
        public Vector3 positionOffset = new Vector3(0f, 0f, -1.5f);

        [Header("Audio")]
#if !UNITY_SERVER
        public AK.Wwise.Event droneLoopEvent;
#endif

        private Transform _followTarget;
        private bool _flying = false;

        private void Awake()
        {
#if !UNITY_SERVER
            if (droneLoopEvent != null && droneLoopEvent.IsValid())
                droneLoopEvent.Post(gameObject);
#endif
        }

        private void Update()
        {
            if (_followTarget != null && !_flying)
                SnapToTarget();
        }

        private void SnapToTarget()
        {
            transform.position = _followTarget.position + _followTarget.TransformDirection(positionOffset);

            Vector3 forward = _followTarget.forward;
            if (forward.sqrMagnitude < 0.001f)
                forward = _followTarget.right;

            Quaternion look = Quaternion.LookRotation(forward.normalized, Vector3.up);

            if (rotationOffset != Vector3.zero)
                look *= Quaternion.Euler(rotationOffset);

            transform.rotation = look;
        }

        public void SetFollowTarget(Transform target)
        {
            _followTarget = target;
        }

        public void SetVisible(bool visible)
        {
            foreach (var r in GetComponentsInChildren<Renderer>())
                r.enabled = visible;
        }

        public void FlyAway(Vector3 direction)
        {
            _followTarget = null;
            _flying = true;
            StartCoroutine(FlyAwayRoutine(direction));
        }

        private IEnumerator FlyAwayRoutine(Vector3 direction)
        {
            float speed = 0f;
            float elapsed = 0f;

            Quaternion lockedRotation = transform.rotation;

            while (elapsed < 3f)
            {
                speed = Mathf.MoveTowards(speed, maxSpeed, acceleration * Time.deltaTime);
                transform.position += direction * speed * Time.deltaTime;
                transform.rotation = lockedRotation;

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
