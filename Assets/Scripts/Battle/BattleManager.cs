using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SuikodenLike.Core;
using SuikodenLike.Data;

namespace SuikodenLike.Battle
{
    /// <summary>
    /// Runs a full turn-based battle. Listens for GameManager.OnBattleRequested,
    /// builds the two sides, resolves turns by speed, handles Attack/Skill/Item/
    /// Defend/Run, awards exp/gold/loot, and reports the result back.
    ///
    /// UI integration: subscribe to the events to render, then call
    /// SubmitPlayerAction() when the player picks a command.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        [SerializeField] float actionDelay = 0.6f;

        public List<BattleUnit> PlayerUnits { get; private set; } = new List<BattleUnit>();
        public List<BattleUnit> EnemyUnits { get; private set; } = new List<BattleUnit>();

        /// <summary>Battle log line for the UI to display.</summary>
        public event Action<string> OnMessage;
        /// <summary>Something changed (HP/MP/death) — refresh the UI.</summary>
        public event Action OnStateChanged;
        /// <summary>It's this player unit's turn — show the command menu.</summary>
        public event Action<BattleUnit> OnAwaitPlayerCommand;
        /// <summary>Battle finished. bool = victory.</summary>
        public event Action<bool> OnBattleFinished;

        BattleAction pendingPlayerAction;
        bool running;

        // Subscribe in Start so GameManager.Awake has definitely assigned Instance.
        void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnBattleRequested += BeginBattle;
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnBattleRequested -= BeginBattle;
        }

        public void BeginBattle(List<EnemyData> enemies)
        {
            if (running) return;
            var gm = GameManager.Instance;

            PlayerUnits = gm.Party.Where(m => m != null).Select(m => new BattleUnit(m)).ToList();
            EnemyUnits = enemies.Where(e => e != null).Select(e => new BattleUnit(e)).ToList();

            StartCoroutine(BattleLoop());
        }

        /// <summary>Called by the UI once the player has chosen a command.</summary>
        public void SubmitPlayerAction(BattleAction action) => pendingPlayerAction = action;

        IEnumerator BattleLoop()
        {
            running = true;
            OnMessage?.Invoke("Battle start!");
            OnStateChanged?.Invoke();
            yield return new WaitForSeconds(actionDelay);

            bool playerRan = false;

            while (AnyAlive(PlayerUnits) && AnyAlive(EnemyUnits) && !playerRan)
            {
                // Turn order: everyone alive, fastest first, re-sorted each round.
                var order = PlayerUnits.Concat(EnemyUnits)
                    .Where(u => u.IsAlive)
                    .OrderByDescending(u => u.Speed)
                    .ToList();

                foreach (var actor in order)
                {
                    if (!actor.IsAlive) continue;
                    if (!AnyAlive(PlayerUnits) || !AnyAlive(EnemyUnits)) break;

                    actor.isDefending = false; // defend lasts until this unit's next turn

                    BattleAction action;
                    if (actor.isPlayer)
                    {
                        pendingPlayerAction = null;
                        OnAwaitPlayerCommand?.Invoke(actor);
                        while (pendingPlayerAction == null) yield return null;
                        action = pendingPlayerAction;
                    }
                    else
                    {
                        action = ChooseEnemyAction(actor);
                    }

                    yield return StartCoroutine(ResolveAction(actor, action, r => playerRan = r));
                    OnStateChanged?.Invoke();
                    yield return new WaitForSeconds(actionDelay);

                    if (playerRan) break;
                }
            }

            bool victory = AnyAlive(PlayerUnits) && !AnyAlive(EnemyUnits);
            yield return StartCoroutine(FinishBattle(victory, playerRan));
        }

        IEnumerator ResolveAction(BattleUnit actor, BattleAction action, Action<bool> setRan)
        {
            switch (action.type)
            {
                case BattleActionType.Attack:
                {
                    var target = EnsureTarget(action.target, EnemyUnits, PlayerUnits, actor);
                    if (target == null) break;
                    if (DamageCalculator.RollHit(actor, target))
                    {
                        int dmg = DamageCalculator.BasicAttack(actor, target, out bool crit);
                        target.TakeDamage(dmg);
                        OnMessage?.Invoke($"{actor.Name} attacks {target.Name} for {dmg}{(crit ? " (critical!)" : "")}.");
                    }
                    else OnMessage?.Invoke($"{actor.Name} missed {target.Name}!");
                    break;
                }
                case BattleActionType.Skill:
                    yield return ResolveSkill(actor, action);
                    break;
                case BattleActionType.Item:
                    ResolveItem(actor, action);
                    break;
                case BattleActionType.Defend:
                    actor.isDefending = true;
                    OnMessage?.Invoke($"{actor.Name} defends.");
                    break;
                case BattleActionType.Run:
                    if (TryRun(actor)) { OnMessage?.Invoke("Got away safely!"); setRan(true); }
                    else OnMessage?.Invoke("Couldn't escape!");
                    break;
            }
            CleanupDead();
            yield return null;
        }

        IEnumerator ResolveSkill(BattleUnit actor, BattleAction action)
        {
            SkillData skill = action.skill;
            if (skill == null) yield break;
            if (actor.currentMP < skill.mpCost)
            {
                OnMessage?.Invoke($"{actor.Name} doesn't have enough MP!");
                yield break;
            }
            actor.SpendMP(skill.mpCost);

            var targets = ResolveTargets(actor, skill, action.target);
            foreach (var t in targets)
            {
                switch (skill.kind)
                {
                    case SkillKind.PhysicalDamage:
                    case SkillKind.MagicDamage:
                        int dmg = DamageCalculator.SkillDamage(actor, t, skill);
                        t.TakeDamage(dmg);
                        OnMessage?.Invoke($"{actor.Name} uses {skill.displayName} on {t.Name} for {dmg}.");
                        break;
                    case SkillKind.Heal:
                        int heal = DamageCalculator.HealAmount(actor, skill);
                        t.Heal(heal);
                        OnMessage?.Invoke($"{actor.Name} heals {t.Name} for {heal}.");
                        break;
                    case SkillKind.Buff:
                        OnMessage?.Invoke($"{actor.Name} uses {skill.displayName}.");
                        break;
                }
            }
        }

        void ResolveItem(BattleUnit actor, BattleAction action)
        {
            var gm = GameManager.Instance;
            ItemData item = action.item;
            if (item == null || !gm.Inventory.Has(item)) return;

            var target = action.target ?? actor;
            switch (item.effect)
            {
                case ItemEffect.HealHP: target.Heal(item.effectAmount); break;
                case ItemEffect.HealMP: target.currentMP = Mathf.Min(target.MaxMP, target.currentMP + item.effectAmount); break;
                case ItemEffect.Revive:
                    if (!target.IsAlive) target.Heal(Mathf.Max(1, item.effectAmount));
                    break;
            }
            gm.Inventory.Remove(item, 1);
            OnMessage?.Invoke($"{actor.Name} uses {item.displayName} on {target.Name}.");
        }

        // --- Enemy AI: attack a random living hero, or heal/skill occasionally ---
        BattleAction ChooseEnemyAction(BattleUnit enemy)
        {
            var livingHeroes = PlayerUnits.Where(u => u.IsAlive).ToList();
            if (livingHeroes.Count == 0) return BattleAction.Defend();

            var damageSkills = enemy.Skills?
                .Where(s => s != null && s.mpCost <= enemy.currentMP &&
                            (s.kind == SkillKind.PhysicalDamage || s.kind == SkillKind.MagicDamage))
                .ToList();

            var target = livingHeroes[UnityEngine.Random.Range(0, livingHeroes.Count)];
            if (damageSkills != null && damageSkills.Count > 0 && UnityEngine.Random.value < 0.4f)
                return BattleAction.Skill_(damageSkills[UnityEngine.Random.Range(0, damageSkills.Count)], target);

            return BattleAction.Attack(target);
        }

        List<BattleUnit> ResolveTargets(BattleUnit actor, SkillData skill, BattleUnit primary)
        {
            var friends = actor.isPlayer ? PlayerUnits : EnemyUnits;
            var foes = actor.isPlayer ? EnemyUnits : PlayerUnits;

            switch (skill.target)
            {
                case SkillTarget.AllEnemies: return foes.Where(u => u.IsAlive).ToList();
                case SkillTarget.AllAllies: return friends.Where(u => u.IsAlive).ToList();
                case SkillTarget.Self: return new List<BattleUnit> { actor };
                case SkillTarget.SingleAlly:
                    return new List<BattleUnit> { primary ?? friends.First(u => u.IsAlive) };
                default: // SingleEnemy
                    return new List<BattleUnit> { EnsureTarget(primary, foes, friends, actor) };
            }
        }

        BattleUnit EnsureTarget(BattleUnit desired, List<BattleUnit> foes, List<BattleUnit> friends, BattleUnit actor)
        {
            if (desired != null && desired.IsAlive) return desired;
            return foes.FirstOrDefault(u => u.IsAlive);
        }

        bool TryRun(BattleUnit actor)
        {
            float avgFoeSpeed = EnemyUnits.Where(u => u.IsAlive).DefaultIfEmpty().Average(u => u?.Speed ?? 0);
            float chance = 0.5f + (actor.Speed - avgFoeSpeed) * 0.05f;
            return UnityEngine.Random.value <= Mathf.Clamp(chance, 0.1f, 0.95f);
        }

        void CleanupDead()
        {
            OnStateChanged?.Invoke();
        }

        IEnumerator FinishBattle(bool victory, bool ran)
        {
            var gm = GameManager.Instance;

            if (victory)
            {
                int exp = EnemyUnits.Sum(u => u.enemy.expReward);
                int gold = EnemyUnits.Sum(u => u.enemy.goldReward);
                gm.Inventory.AddGold(gold);
                OnMessage?.Invoke($"Victory! Gained {exp} EXP and {gold} gold.");

                foreach (var hero in PlayerUnits.Where(h => h.IsAlive))
                {
                    if (hero.member.GainExp(exp))
                        OnMessage?.Invoke($"{hero.Name} reached level {hero.member.level}!");
                }
                // Roll loot.
                foreach (var e in EnemyUnits)
                    foreach (var drop in e.enemy.loot)
                        if (drop.item != null && UnityEngine.Random.value <= drop.chance)
                        {
                            gm.Inventory.Add(drop.item, 1);
                            OnMessage?.Invoke($"Found {drop.item.displayName}!");
                        }
            }
            else if (!ran)
            {
                OnMessage?.Invoke("The party was defeated...");
            }

            // Persist battle HP/MP back onto the real party members.
            foreach (var hero in PlayerUnits) hero.SyncToMember();

            yield return new WaitForSeconds(actionDelay);

            running = false;
            OnBattleFinished?.Invoke(victory);
            gm.EndBattle(victory);
        }

        static bool AnyAlive(List<BattleUnit> units) => units.Any(u => u.IsAlive);
    }
}
