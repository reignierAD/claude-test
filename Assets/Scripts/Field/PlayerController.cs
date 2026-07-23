using UnityEngine;

namespace SuikodenLike.Field
{
    /// <summary>
    /// Top-down 2D overworld movement (Suikoden-style free 8-direction walk).
    /// Requires a Rigidbody2D (set to Kinematic or Dynamic with no gravity)
    /// and a Collider2D. Reports distance walked so the encounter system can
    /// trigger fights, and remembers facing for interaction.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 4f;
        [SerializeField] Animator animator; // optional; drives your pixel walk cycle

        Rigidbody2D rb;
        Vector2 input;
        Vector2 facing = Vector2.down;

        /// <summary>World distance walked this session; encounter system reads & resets it.</summary>
        public float DistanceWalked { get; private set; }
        public Vector2 Facing => facing;
        public bool ControlEnabled { get; set; } = true;

        void Awake() => rb = GetComponent<Rigidbody2D>();

        void Update()
        {
            if (!ControlEnabled)
            {
                input = Vector2.zero;
                if (animator) animator.SetFloat("Speed", 0f);
                return;
            }

            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");
            if (input.sqrMagnitude > 1f) input.Normalize();

            if (input.sqrMagnitude > 0.01f)
            {
                facing = input;
                if (animator)
                {
                    animator.SetFloat("MoveX", facing.x);
                    animator.SetFloat("MoveY", facing.y);
                }
            }
            if (animator) animator.SetFloat("Speed", input.magnitude);
        }

        void FixedUpdate()
        {
            if (!ControlEnabled) return;
            Vector2 delta = input * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + delta);
            DistanceWalked += delta.magnitude;
        }

        public void ResetDistanceWalked() => DistanceWalked = 0f;
    }
}
