using UnityEngine;

namespace SuikodenLike.Field
{
    /// <summary>
    /// Put this on the player. On the interact button it probes just in front
    /// of the player (in the facing direction) and triggers the nearest
    /// Interactable — NPC dialogue, chests, signs, doors, etc.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] KeyCode interactKey = KeyCode.E;
        [SerializeField] float reach = 0.9f;
        [SerializeField] float radius = 0.35f;
        [SerializeField] LayerMask interactableMask = ~0;

        PlayerController player;

        void Awake() => player = GetComponent<PlayerController>();

        void Update()
        {
            if (!player.ControlEnabled) return;
            if (!Input.GetKeyDown(interactKey) && !Input.GetButtonDown("Submit")) return;

            Vector2 origin = (Vector2)transform.position + player.Facing * reach;

            // OverlapCircle returns an arbitrary single collider, which is
            // often our own body or a piece of scenery — gather them all and
            // take the nearest one that's actually interactable.
            var hits = Physics2D.OverlapCircleAll(origin, radius, interactableMask);
            Interactable best = null;
            float bestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                var candidate = hit.GetComponentInParent<Interactable>();
                if (candidate == null) continue;
                if (candidate.transform.IsChildOf(transform)) continue; // ignore ourselves

                float d = Vector2.SqrMagnitude((Vector2)candidate.transform.position - origin);
                if (d < bestDistance) { bestDistance = d; best = candidate; }
            }

            if (best != null) best.Interact(player);
        }

        void OnDrawGizmosSelected()
        {
            var pc = GetComponent<PlayerController>();
            if (pc == null) return;
            Gizmos.color = Color.cyan;
            Vector2 origin = (Vector2)transform.position + pc.Facing * reach;
            Gizmos.DrawWireSphere(origin, radius);
        }
    }
}
