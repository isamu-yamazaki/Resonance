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

        [Tooltip("How much of the spline node's pitch the drone inherits while circling. " +
                 "0 = fully flat, 1 = full node pitch.")]
        [Range(0f, 1f)]
        public float pitchInfluence = 0.25f;

        private Transform _followTarget;
        private bool _flying = false;

        private void Update()
        {
            if (_followTarget != null && !_flying)
                SnapToTarget();
        }

        private void SnapToTarget()
        {
            transform.position = _followTarget.position;

            Vector3 forward = _followTarget.forward;
            if (forward.sqrMagnitude < 0.001f)
                forward = _followTarget.right;
            forward.Normalize();

            Vector3 flatForward = new Vector3(forward.x, 0f, forward.z);
            if (flatForward.sqrMagnitude < 0.001f) flatForward = Vector3.forward;
            Quaternion flatRot = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
            Quaternion fullRot = Quaternion.LookRotation(forward, Vector3.up);

            Quaternion look = Quaternion.Slerp(flatRot, fullRot, pitchInfluence);

            if (rotationOffset != Vector3.zero)
                look *= Quaternion.Euler(rotationOffset);

            transform.rotation = look;
        }

        public void SetFollowTarget(Transform target)
        {
            _followTarget = target;
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

            // Lock rotation to whatever it was at cinematic end — no turning at all.
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

        private static Quaternion FlattenToYaw(Quaternion q)
        {
            Vector3 flat = q * Vector3.forward;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.001f) flat = Vector3.forward;
            return Quaternion.LookRotation(flat.normalized, Vector3.up);
        }
    }
}
