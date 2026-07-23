using UnityEngine;
using SuikodenLike.Core;
using SuikodenLike.Data;
using SuikodenLike.Dialogue;

namespace SuikodenLike.Field
{
    /// <summary>
    /// A placeable NPC. Drop the prefab in the scene, assign a sprite and a
    /// DialogueData asset, and it talks — no code. Optionally shows a
    /// different conversation once a story flag is set.
    /// </summary>
    public class NPCInteractable : Interactable
    {
        [Header("Conversation")]
        [SerializeField] DialogueData dialogue;

        [Header("Optional: flag-gated alternate line")]
        [SerializeField] string requiresFlag;
        [SerializeField] DialogueData dialogueWhenFlagSet;

        [Header("Face the player when talked to")]
        [SerializeField] SpriteRenderer spriteRenderer;

        public override void Interact(PlayerController player)
        {
            var gm = GameManager.Instance;
            DialogueData toPlay = dialogue;
            if (gm != null && !string.IsNullOrEmpty(requiresFlag) &&
                gm.HasFlag(requiresFlag) && dialogueWhenFlagSet != null)
            {
                toPlay = dialogueWhenFlagSet;
            }

            if (toPlay == null || DialogueManager.Instance == null) return;
            DialogueManager.Instance.StartDialogue(toPlay, player);
        }
    }
}
