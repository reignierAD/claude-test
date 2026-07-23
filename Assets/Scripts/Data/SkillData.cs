using UnityEngine;

namespace SuikodenLike.Data
{
    public enum SkillTarget { SingleEnemy, AllEnemies, SingleAlly, AllAllies, Self }
    public enum SkillKind { PhysicalDamage, MagicDamage, Heal, Buff }

    /// <summary>
    /// A battle command / spell. Fully data-driven so you can invent
    /// your own skills without writing code.
    /// Create via: Assets > Create > SuikodenLike > Skill.
    /// </summary>
    [CreateAssetMenu(fileName = "New Skill", menuName = "SuikodenLike/Skill")]
    public class SkillData : ScriptableObject
    {
        public string skillId = "attack";
        public string displayName = "Attack";
        [TextArea] public string description;
        public Sprite icon;

        public SkillKind kind = SkillKind.PhysicalDamage;
        public SkillTarget target = SkillTarget.SingleEnemy;

        [Tooltip("MP cost. 0 = free (basic attack).")]
        public int mpCost = 0;

        [Tooltip("Base power. Scales with strength (physical) or magic (magic/heal).")]
        public int power = 10;

        [Tooltip("Optional VFX prefab spawned on the target when this resolves.")]
        public GameObject hitEffectPrefab;
    }
}
