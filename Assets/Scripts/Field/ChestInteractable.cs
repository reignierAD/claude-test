using UnityEngine;
using SuikodenLike.Core;
using SuikodenLike.Data;
using SuikodenLike.Dialogue;

namespace SuikodenLike.Field
{
    /// <summary>
    /// A placeable treasure chest / lootable object. Gives an item (and/or
    /// gold) once, then stays open. Example of a non-NPC interactable object.
    /// </summary>
    public class ChestInteractable : Interactable
    {
        [SerializeField] ItemData item;
        [SerializeField] int quantity = 1;
        [SerializeField] int gold = 0;

        [Header("Visuals")]
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite openedSprite;

        [Header("Persistence")]
        [Tooltip("Unique id so this chest stays opened via a story flag.")]
        [SerializeField] string chestId = "chest_001";

        bool opened;

        void Start()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.HasFlag(FlagKey)) SetOpenedVisual();
        }

        string FlagKey => $"chest_opened:{chestId}";

        public override void Interact(PlayerController player)
        {
            if (opened) return;
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (item != null) gm.Inventory.Add(item, quantity);
            if (gold > 0) gm.Inventory.AddGold(gold);
            gm.SetFlag(FlagKey);
            SetOpenedVisual();

            string what = item != null ? $"{item.displayName} x{quantity}" : $"{gold} gold";
            DialogueManager.Instance?.ShowSystemLine($"Found {what}!", player);
        }

        void SetOpenedVisual()
        {
            opened = true;
            if (spriteRenderer && openedSprite) spriteRenderer.sprite = openedSprite;
        }
    }
}
