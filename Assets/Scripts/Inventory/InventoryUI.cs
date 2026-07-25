using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SuikodenLike.Core;

namespace SuikodenLike.Inventory
{
    /// <summary>
    /// A simple list view of the player's items + gold. Rebuilds itself
    /// whenever the inventory changes. Wire a container + row prefab in the
    /// editor. Toggle open/close with a key from your field UI.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Transform listContainer;
        [SerializeField] GameObject rowPrefab;   // needs a TMP_Text + optional Image
        [SerializeField] TMP_Text goldText;
        [Tooltip("Press to open/close the bag in the field. None = no hotkey.")]
        [SerializeField] KeyCode toggleKey = KeyCode.I;

        Inventory inventory;

        void Start()
        {
            inventory = GameManager.Instance?.Inventory;
            if (inventory != null) inventory.OnChanged += Rebuild;
            Rebuild();
            if (root) root.SetActive(false);
        }

        void OnDestroy()
        {
            if (inventory != null) inventory.OnChanged -= Rebuild;
        }

        void Update()
        {
            if (toggleKey == KeyCode.None) return;
            // Don't let the bag open mid-conversation or mid-battle.
            var gm = GameManager.Instance;
            if (gm != null && gm.InBattle) return;
            if (Dialogue.DialogueManager.Instance != null &&
                Dialogue.DialogueManager.Instance.IsRunning) return;
            if (Input.GetKeyDown(toggleKey)) Toggle();
        }

        public void Toggle()
        {
            if (root == null) return;
            root.SetActive(!root.activeSelf);
            if (root.activeSelf) Rebuild();
        }

        void Rebuild()
        {
            if (inventory == null || listContainer == null || rowPrefab == null) return;

            for (int i = listContainer.childCount - 1; i >= 0; i--)
                Destroy(listContainer.GetChild(i).gameObject);

            foreach (var slot in inventory.Slots)
            {
                var row = Instantiate(rowPrefab, listContainer);
                var text = row.GetComponentInChildren<TMP_Text>();
                if (text) text.text = slot.item.stackable
                    ? $"{slot.item.displayName}  x{slot.count}"
                    : slot.item.displayName;
                var img = row.GetComponentInChildren<Image>();
                if (img && slot.item.icon) img.sprite = slot.item.icon;
            }

            if (goldText) goldText.text = $"Gold: {inventory.Gold}";
        }
    }
}
