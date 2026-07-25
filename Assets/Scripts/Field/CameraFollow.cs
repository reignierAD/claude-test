using UnityEngine;

namespace SuikodenLike.Field
{
    /// <summary>
    /// Smoothly keeps the camera on the player, clamped so it never shows the
    /// void past the edges of the map. Disable this component to park the
    /// camera somewhere else (the battle arena does exactly that).
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] float smoothTime = 0.12f;

        [Header("Map bounds (world units)")]
        [Tooltip("Leave both at zero to disable clamping.")]
        [SerializeField] Vector2 mapMin = Vector2.zero;
        [SerializeField] Vector2 mapMax = Vector2.zero;

        Vector3 velocity;
        Camera cam;

        void Awake() => cam = GetComponent<Camera>();

        public void SetTarget(Transform t) => target = t;

        public void SetBounds(Vector2 min, Vector2 max)
        {
            mapMin = min;
            mapMax = max;
        }

        void LateUpdate()
        {
            if (target == null) return;

            Vector3 goal = new Vector3(target.position.x, target.position.y, transform.position.z);

            if (cam != null && cam.orthographic && mapMax != mapMin)
            {
                float halfH = cam.orthographicSize;
                float halfW = halfH * cam.aspect;

                // If the map is smaller than the view, just centre on it.
                goal.x = (mapMax.x - mapMin.x) <= halfW * 2f
                    ? (mapMin.x + mapMax.x) * 0.5f
                    : Mathf.Clamp(goal.x, mapMin.x + halfW, mapMax.x - halfW);

                goal.y = (mapMax.y - mapMin.y) <= halfH * 2f
                    ? (mapMin.y + mapMax.y) * 0.5f
                    : Mathf.Clamp(goal.y, mapMin.y + halfH, mapMax.y - halfH);
            }

            transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
        }

        /// <summary>Jump straight to the target with no easing (scene starts, warps).</summary>
        public void SnapToTarget()
        {
            if (target == null) return;
            velocity = Vector3.zero;
            transform.position = new Vector3(target.position.x, target.position.y, transform.position.z);
        }
    }
}
