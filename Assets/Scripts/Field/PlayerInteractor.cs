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
            Collider2D hit = Physics2D.OverlapCircle(origin, radius, interactableMask);
            if (hit == null) return;

            var interactable = hit.GetComponentInParent<Interactable>();
            if (interactable != null) interactable.Interact(player);
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
