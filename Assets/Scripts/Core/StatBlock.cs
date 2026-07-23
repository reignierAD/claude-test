using System;
using UnityEngine;

namespace SuikodenLike.Core
{
    /// <summary>
    /// Shared stat container used by both party characters and enemies.
    /// Kept as a plain serializable struct-like class so it can live inside
    /// ScriptableObjects (designer-authored) and be copied into runtime units.
    /// </summary>
    [Serializable]
    public class StatBlock
    {
        [Min(1)] public int maxHP = 20;
        [Min(0)] public int maxMP = 0;

        [Tooltip("Physical attack power.")]
        public int strength = 5;
        [Tooltip("Reduces incoming physical damage.")]
        public int defense = 3;
        [Tooltip("Magic power / affects skill damage.")]
        public int magic = 5;
        [Tooltip("Magic resistance.")]
        public int magicDefense = 3;
        [Tooltip("Turn order & hit/evade rolls.")]
        public int speed = 5;
        [Tooltip("Affects hit and critical chance.")]
        public int technique = 5;
        [Tooltip("Affects evade and luck-based rolls.")]
        public int luck = 5;

        public StatBlock Clone()
        {
            return new StatBlock
            {
                maxHP = maxHP,
                maxMP = maxMP,
                strength = strength,
                defense = defense,
                magic = magic,
                magicDefense = magicDefense,
                speed = speed,
                technique = technique,
                luck = luck
            };
        }
    }
}
