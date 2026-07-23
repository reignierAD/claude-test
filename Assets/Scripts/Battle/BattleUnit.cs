using System.Collections.Generic;
using UnityEngine;
using SuikodenLike.Data;
using SuikodenLike.Party;

namespace SuikodenLike.Battle
{
    /// <summary>
    /// A single combatant in a battle. Wraps either a party PartyMember or an
    /// EnemyData, exposing a uniform interface (HP, stats, skills) so the
    /// BattleManager can treat both sides the same.
    /// </summary>
    public class BattleUnit
    {
        public readonly bool isPlayer;
        public readonly PartyMember member;   // set when isPlayer
        public readonly EnemyData enemy;      // set when !isPlayer

        public int currentHP;
        public int currentMP;
        public bool isDefending;

        public BattleUnit(PartyMember member)
        {
            isPlayer = true;
            this.member = member;
            currentHP = member.currentHP;
            currentMP = member.currentMP;
        }

        public BattleUnit(EnemyData enemy)
        {
            isPlayer = false;
            this.enemy = enemy;
            currentHP = enemy.stats.maxHP;
            currentMP = enemy.stats.maxMP;
        }

        public string Name => isPlayer ? member.DisplayName : enemy.displayName;
        public bool IsAlive => currentHP > 0;

        public int MaxHP => isPlayer ? member.MaxHP : enemy.stats.maxHP;
        public int MaxMP => isPlayer ? member.MaxMP : enemy.stats.maxMP;
        public int Strength => isPlayer ? member.Strength : enemy.stats.strength;
        public int Defense => isPlayer ? member.Defense : enemy.stats.defense;
        public int Magic => isPlayer ? member.Magic : enemy.stats.magic;
        public int MagicDefense => isPlayer ? member.MagicDefense : enemy.stats.magicDefense;
        public int Speed => isPlayer ? member.Speed : enemy.stats.speed;
        public int Technique => isPlayer ? member.Technique : enemy.stats.technique;
        public int Luck => isPlayer ? member.Luck : enemy.stats.luck;

        public IReadOnlyList<SkillData> Skills =>
            isPlayer ? member.Skills : (IReadOnlyList<SkillData>)enemy.skills;

        public void TakeDamage(int amount) => currentHP = Mathf.Max(0, currentHP - amount);
        public void Heal(int amount) => currentHP = Mathf.Min(MaxHP, currentHP + amount);
        public void SpendMP(int amount) => currentMP = Mathf.Max(0, currentMP - amount);

        /// <summary>Write battle HP/MP back to the persistent party member.</summary>
        public void SyncToMember()
        {
            if (!isPlayer) return;
            member.currentHP = currentHP;
            member.currentMP = currentMP;
        }
    }
}
