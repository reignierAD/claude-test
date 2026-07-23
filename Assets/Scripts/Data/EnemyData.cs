using System;
using System.Collections.Generic;
using UnityEngine;
using SuikodenLike.Core;

namespace SuikodenLike.Data
{
    [Serializable]
    public class LootDrop
    {
        public ItemData item;
        [Range(0f, 1f)] public float chance = 0.25f;
    }

    /// <summary>
    /// Designer-authored enemy. Drop in your pixel sprite, tune stats and loot.
    /// Create via: Assets > Create > SuikodenLike > Enemy.
    /// </summary>
    [CreateAssetMenu(fileName = "New Enemy", menuName = "SuikodenLike/Enemy")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public string enemyId = "slime";
        public string displayName = "Slime";
        public Sprite sprite;

        [Header("Combat")]
        public StatBlock stats = new StatBlock();
        [Tooltip("Skills the enemy may use. Empty = basic attack only.")]
        public List<SkillData> skills = new List<SkillData>();

        [Header("Rewards")]
        public int expReward = 5;
        public int goldReward = 10;
        public List<LootDrop> loot = new List<LootDrop>();
    }
}
