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
            float elapsed = 0f;

            while (elapsed < 3f)
            {
                speed = Mathf.MoveTowards(speed, maxSpeed, acceleration * Time.deltaTime);
                transform.position += direction * speed * Time.deltaTime;
                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
