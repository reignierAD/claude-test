using System.Collections.Generic;
using UnityEngine;
using SuikodenLike.Core;
using SuikodenLike.Data;

namespace SuikodenLike.Party
{
    /// <summary>
    /// Runtime instance of a character: the authored CharacterData plus
    /// current HP/MP, level, exp and equipment. This is what actually
    /// fights and levels up.
    /// </summary>
    public class PartyMember
    {
        public readonly CharacterData data;
        public int level;
        public int exp;

        public int currentHP;
        public int currentMP;

        public ItemData equippedWeapon;
        public ItemData equippedArmor;

        public PartyMember(CharacterData data)
        {
            this.data = data;
            level = Mathf.Max(1, data.startingLevel);
            RecomputeAndFullHeal();
        }

        public string DisplayName => data != null ? data.displayName : "???";
        public bool IsAlive => currentHP > 0;
        public IReadOnlyList<SkillData> Skills => data.skills;

        // --- Derived stats (base + per-level growth + equipment) ---
        public int MaxHP => data.baseStats.maxHP + data.hpPerLevel * (level - 1);
        public int MaxMP => data.baseStats.maxMP + data.mpPerLevel * (level - 1);
        public int Strength => data.baseStats.strength + data.strengthPerLevel * (level - 1)
                               + (equippedWeapon ? equippedWeapon.attackBonus : 0);
        public int Defense => data.baseStats.defense + data.defensePerLevel * (level - 1)
                              + (equippedArmor ? equippedArmor.defenseBonus : 0);
        public int Magic => data.baseStats.magic;
        public int MagicDefense => data.baseStats.magicDefense;
        public int Speed => data.baseStats.speed;
        public int Technique => data.baseStats.technique;
        public int Luck => data.baseStats.luck;

        public int ExpToNextLevel => 20 + (level - 1) * 15;

        public void RecomputeAndFullHeal()
        {
            currentHP = MaxHP;
            currentMP = MaxMP;
        }

        public void Heal(int amount) => currentHP = Mathf.Min(MaxHP, currentHP + amount);
        public void RestoreMP(int amount) => currentMP = Mathf.Min(MaxMP, currentMP + amount);
        public void TakeDamage(int amount) => currentHP = Mathf.Max(0, currentHP - amount);
        public void Revive(int hp) => currentHP = Mathf.Clamp(hp, 1, MaxHP);

        /// <summary>Grant exp; returns true if at least one level was gained.</summary>
        public bool GainExp(int amount)
        {
            exp += amount;
            bool leveled = false;
            while (exp >= ExpToNextLevel)
            {
                exp -= ExpToNextLevel;
                level++;
                leveled = true;
                // Level-up heals the gained HP/MP.
                currentHP += data.hpPerLevel;
                currentMP += data.mpPerLevel;
            }
            currentHP = Mathf.Min(currentHP, MaxHP);
            currentMP = Mathf.Min(currentMP, MaxMP);
            return leveled;
        }
    }
}
