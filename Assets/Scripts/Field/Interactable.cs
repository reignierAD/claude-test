using UnityEngine;

namespace SuikodenLike.Field
{
    /// <summary>
    /// Anything the player can walk up to and press the interact button on:
    /// NPCs, signs, chests, doors. The PlayerInteractor finds the nearest
    /// Interactable in front of the player and calls Interact().
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        [SerializeField] protected string prompt = "Talk";
        public string Prompt => prompt;

        /// <summary>Invoked when the player interacts while facing this object.</summary>
        public abstract void Interact(PlayerController player);
    }
}
