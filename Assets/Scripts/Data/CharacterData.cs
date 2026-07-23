using System.Collections.Generic;
using UnityEngine;
using SuikodenLike.Core;

namespace SuikodenLike.Data
{
    /// <summary>
    /// Designer-authored definition of a playable character.
    /// Create via: Assets > Create > SuikodenLike > Character.
    /// Drop in your own pixel sprite + portrait and stats — no code needed.
    /// </summary>
    [CreateAssetMenu(fileName = "New Character", menuName = "SuikodenLike/Character")]
    public class CharacterData : ScriptableObject
    {
        [Header("Identity")]
        public string characterId = "hero";
        public string displayName = "Hero";
        [TextArea] public string bio;

        [Header("Art (your pixel assets)")]
        [Tooltip("Overworld/battle sprite.")]
        public Sprite sprite;
        [Tooltip("Dialogue portrait shown in text boxes.")]
        public Sprite portrait;

        [Header("Combat")]
        public StatBlock baseStats = new StatBlock();
        [Tooltip("Skills/spells this character knows.")]
        public List<SkillData> skills = new List<SkillData>();

        [Header("Progression")]
        public int startingLevel = 1;
        [Tooltip("Simple curve: HP gained per level, etc. Tune per character.")]
        public int hpPerLevel = 4;
        public int mpPerLevel = 1;
        public int strengthPerLevel = 1;
        public int defensePerLevel = 1;
    }
}
