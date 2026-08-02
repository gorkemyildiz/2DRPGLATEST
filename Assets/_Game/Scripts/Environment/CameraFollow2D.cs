using UnityEngine;

namespace Game.Environment
{
    /// <summary>
    /// Keeps the orthographic camera locked on the hero during battle travel/combat.
    /// </summary>
    public class CameraFollow2D : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Follow")]
        [SerializeField] private bool followX = true;
        [SerializeField] private bool followY = false;
        [SerializeField] private Vector3 offset = new Vector3(1.2f, 0f, -10f);
        [SerializeField] private float smoothTime = 0.12f;

        private Vector3 velocity;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                SnapToTarget();
            }
        }

        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            transform.position = BuildDesiredPosition(transform.position);
            velocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desired = BuildDesiredPosition(transform.position);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        }

        private Vector3 BuildDesiredPosition(Vector3 current)
        {
            Vector3 desired = current;
            Vector3 targetPos = target.position;

            if (followX)
            {
                desired.x = targetPos.x + offset.x;
            }

            if (followY)
            {
                desired.y = targetPos.y + offset.y;
            }
            else
            {
                desired.y = offset.y;
            }

            desired.z = offset.z;
            return desired;
        }
    }
}
