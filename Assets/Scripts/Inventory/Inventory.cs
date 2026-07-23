using System;
using System.Collections.Generic;
using SuikodenLike.Data;

namespace SuikodenLike.Inventory
{
    [Serializable]
    public class InventorySlot
    {
        public ItemData item;
        public int count;
        public InventorySlot(ItemData item, int count) { this.item = item; this.count = count; }
    }

    /// <summary>
    /// Plain (non-MonoBehaviour) inventory model. Owned by GameManager so
    /// it persists across field/battle/scene changes. Fires OnChanged so any
    /// UI can refresh itself.
    /// </summary>
    public class Inventory
    {
        readonly List<InventorySlot> slots = new List<InventorySlot>();
        public IReadOnlyList<InventorySlot> Slots => slots;
        public int Gold { get; private set; }

        public event Action OnChanged;

        public void AddGold(int amount)
        {
            Gold = Math.Max(0, Gold + amount);
            OnChanged?.Invoke();
        }

        public bool SpendGold(int amount)
        {
            if (amount > Gold) return false;
            Gold -= amount;
            OnChanged?.Invoke();
            return true;
        }

        public void Add(ItemData item, int count = 1)
        {
            if (item == null || count <= 0) return;

            if (item.stackable)
            {
                foreach (var slot in slots)
                {
                    if (slot.item == item && slot.count < item.maxStack)
                    {
                        int space = item.maxStack - slot.count;
                        int add = Math.Min(space, count);
                        slot.count += add;
                        count -= add;
                        if (count <= 0) { OnChanged?.Invoke(); return; }
                    }
                }
            }
            while (count > 0)
            {
                int add = item.stackable ? Math.Min(item.maxStack, count) : 1;
                slots.Add(new InventorySlot(item, add));
                count -= add;
            }
            OnChanged?.Invoke();
        }

        public bool Has(ItemData item, int count = 1) => Count(item) >= count;

        public int Count(ItemData item)
        {
            int total = 0;
            foreach (var s in slots) if (s.item == item) total += s.count;
            return total;
        }

        public bool Remove(ItemData item, int count = 1)
        {
            if (!Has(item, count)) return false;
            for (int i = slots.Count - 1; i >= 0 && count > 0; i--)
            {
                if (slots[i].item != item) continue;
                int take = Math.Min(slots[i].count, count);
                slots[i].count -= take;
                count -= take;
                if (slots[i].count <= 0) slots.RemoveAt(i);
            }
            OnChanged?.Invoke();
            return true;
        }
    }
}
