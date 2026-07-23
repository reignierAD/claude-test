using UnityEngine;
using SuikodenLike.Data;

namespace SuikodenLike.Field
{
    /// <summary>
    /// A trigger volume (e.g. tall grass, a dungeon floor) that enables random
    /// encounters while the player stands in it. Assign an EncounterTable and
    /// drop a BoxCollider2D (IsTrigger = true) around the area.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EncounterZone : MonoBehaviour
    {
        [SerializeField] EncounterTable table;
        public EncounterTable Table => table;

        void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col) col.isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var enc = other.GetComponent<RandomEncounterController>();
            if (enc != null) enc.EnterZone(this);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var enc = other.GetComponent<RandomEncounterController>();
            if (enc != null) enc.ExitZone(this);
        }
    }
}
