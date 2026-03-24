using System.Collections;
using UnityEngine;

namespace Resonance
{
    public class Drone : MonoBehaviour
    {
        [Header("Fly Away")]
        public float acceleration = 12f;
        public float maxSpeed = 22f;
        public float tiltSpeed = 4f;

        private Transform followTarget;
        private bool flying = false;

        private void Update()
        {
            if (followTarget != null && !flying)
            {
                transform.position = followTarget.position;
                transform.rotation = followTarget.rotation;
            }
        }

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
        }

        public void FlyAway(Vector3 direction)
        {
            followTarget = null;
            flying = true;
            StartCoroutine(FlyAwayRoutine(direction));
        }

        private IEnumerator FlyAwayRoutine(Vector3 direction)
        {
            float speed = 0f;
            Vector3 flyDirection = (direction + Vector3.up * 0.4f).normalized;
            Quaternion targetTilt = Quaternion.LookRotation(flyDirection);

            while (true)
            {
                speed = Mathf.MoveTowards(speed, maxSpeed, acceleration * Time.deltaTime);
                transform.position += flyDirection * speed * Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetTilt, Time.deltaTime * tiltSpeed);
                yield return null;
            }
        }
    }
}
