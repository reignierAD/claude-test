using UnityEngine;
using SuikodenLike.Data;

namespace SuikodenLike.Battle
{
    /// <summary>
    /// Central place for all combat math. Tweak the formulas here to change
    /// game feel without touching the turn logic.
    /// </summary>
    public static class DamageCalculator
    {
        public static bool RollHit(BattleUnit attacker, BattleUnit defender)
        {
            // Base 90% hit, modified by technique vs speed.
            float chance = 0.9f + (attacker.Technique - defender.Speed) * 0.01f;
            return Random.value <= Mathf.Clamp(chance, 0.3f, 0.99f);
        }

        public static bool RollCrit(BattleUnit attacker)
        {
            float chance = 0.03f + attacker.Luck * 0.005f;
            return Random.value <= Mathf.Clamp(chance, 0f, 0.5f);
        }

        public static int BasicAttack(BattleUnit attacker, BattleUnit defender, out bool crit)
        {
            crit = RollCrit(attacker);
            int raw = attacker.Strength * 2 - defender.Defense;
            raw = Mathf.Max(1, raw);
            if (crit) raw = Mathf.RoundToInt(raw * 1.5f);
            if (defender.isDefending) raw = Mathf.Max(1, raw / 2);
            return ApplyVariance(raw);
        }

        public static int SkillDamage(BattleUnit attacker, BattleUnit defender, SkillData skill)
        {
            int raw;
            if (skill.kind == SkillKind.MagicDamage)
                raw = skill.power + attacker.Magic * 2 - defender.MagicDefense;
            else // physical skill
                raw = skill.power + attacker.Strength - defender.Defense;

            raw = Mathf.Max(1, raw);
            if (defender.isDefending) raw = Mathf.Max(1, raw / 2);
            return ApplyVariance(raw);
        }

        public static int HealAmount(BattleUnit caster, SkillData skill)
        {
            return skill.power + caster.Magic;
        }

        static int ApplyVariance(int value)
        {
            float v = Random.Range(0.9f, 1.1f);
            return Mathf.Max(1, Mathf.RoundToInt(value * v));
        }
    }
}
