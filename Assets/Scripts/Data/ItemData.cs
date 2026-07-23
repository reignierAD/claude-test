using UnityEngine;

namespace SuikodenLike.Data
{
    public enum ItemType { Consumable, Weapon, Armor, KeyItem, Material }
    public enum ItemEffect { None, HealHP, HealMP, Revive, CureStatus }

    /// <summary>
    /// Any item that can live in the inventory. Weapons/armor carry stat
    /// bonuses; consumables carry a battle/field effect.
    /// Create via: Assets > Create > SuikodenLike > Item.
    /// </summary>
    [CreateAssetMenu(fileName = "New Item", menuName = "SuikodenLike/Item")]
    public class ItemData : ScriptableObject
    {
        [Header("Identity")]
        public string itemId = "medicine";
        public string displayName = "Medicine";
        [TextArea] public string description;
        public Sprite icon;

        [Header("Rules")]
        public ItemType type = ItemType.Consumable;
        public bool usableInBattle = true;
        public bool usableInField = true;
        public bool stackable = true;
        [Min(1)] public int maxStack = 99;
        public int buyPrice = 100;
        public int sellPrice = 50;

        [Header("Consumable effect")]
        public ItemEffect effect = ItemEffect.HealHP;
        [Tooltip("Magnitude of the effect (HP restored, etc.).")]
        public int effectAmount = 50;

        [Header("Equipment bonuses (Weapon/Armor)")]
        public int attackBonus = 0;
        public int defenseBonus = 0;
    }
}
