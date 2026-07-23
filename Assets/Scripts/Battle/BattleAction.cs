using SuikodenLike.Data;

namespace SuikodenLike.Battle
{
    public enum BattleActionType { Attack, Skill, Item, Defend, Run }

    /// <summary>
    /// The decision a combatant makes on its turn. The UI builds one of these
    /// from the player's menu selection; enemy AI builds one directly.
    /// </summary>
    public class BattleAction
    {
        public BattleActionType type;
        public SkillData skill;      // for Skill
        public ItemData item;        // for Item
        public BattleUnit target;    // primary target (single-target actions)

        public static BattleAction Attack(BattleUnit target) =>
            new BattleAction { type = BattleActionType.Attack, target = target };
        public static BattleAction Skill_(SkillData skill, BattleUnit target) =>
            new BattleAction { type = BattleActionType.Skill, skill = skill, target = target };
        public static BattleAction Item_(ItemData item, BattleUnit target) =>
            new BattleAction { type = BattleActionType.Item, item = item, target = target };
        public static BattleAction Defend() =>
            new BattleAction { type = BattleActionType.Defend };
        public static BattleAction Run() =>
            new BattleAction { type = BattleActionType.Run };
    }
}
