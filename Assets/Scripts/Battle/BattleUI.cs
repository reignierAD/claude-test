using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SuikodenLike.Core;
using SuikodenLike.Data;

namespace SuikodenLike.Battle
{
    /// <summary>
    /// A minimal, wire-it-in-the-editor battle UI. Shows the log, HP/MP for the
    /// party, a command menu (Attack / Skill / Item / Defend / Run), and target
    /// buttons for the enemies. Talks to BattleManager purely through its public
    /// events + SubmitPlayerAction(), so you can freely restyle or replace it.
    /// </summary>
    public class BattleUI : MonoBehaviour
    {
        [SerializeField] BattleManager battle;
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text logText;
        [SerializeField] TMP_Text partyStatusText;

        [Header("Command menu")]
        [SerializeField] GameObject commandMenu;
        [SerializeField] Button attackButton;
        [SerializeField] Button skillButton;
        [SerializeField] Button itemButton;
        [SerializeField] Button defendButton;
        [SerializeField] Button runButton;

        [Header("Target picker")]
        [SerializeField] Transform targetContainer;
        [SerializeField] Button targetButtonPrefab;

        BattleUnit currentActor;
        readonly List<Button> targetButtons = new List<Button>();

        void OnEnable()
        {
            if (battle == null) return;
            battle.OnMessage += AppendLog;
            battle.OnStateChanged += RefreshStatus;
            battle.OnAwaitPlayerCommand += ShowCommandMenu;
            battle.OnBattleFinished += HandleFinished;

            if (attackButton) attackButton.onClick.AddListener(() => BeginTargeting(BattleActionType.Attack, null, null));
            if (defendButton) defendButton.onClick.AddListener(() => Submit(BattleAction.Defend()));
            if (runButton) runButton.onClick.AddListener(() => Submit(BattleAction.Run()));
            // Skill/Item open sub-menus you can expand; wired minimally here.
            if (skillButton) skillButton.onClick.AddListener(OpenFirstSkill);
            if (itemButton) itemButton.onClick.AddListener(OpenFirstItem);

            if (root) root.SetActive(false);
        }

        void OnDisable()
        {
            if (battle == null) return;
            battle.OnMessage -= AppendLog;
            battle.OnStateChanged -= RefreshStatus;
            battle.OnAwaitPlayerCommand -= ShowCommandMenu;
            battle.OnBattleFinished -= HandleFinished;
        }

        void AppendLog(string msg)
        {
            if (root) root.SetActive(true);
            if (logText) logText.text = msg;
            Debug.Log($"[Battle] {msg}");
        }

        void RefreshStatus()
        {
            if (partyStatusText == null || battle == null) return;
            var sb = new System.Text.StringBuilder();
            foreach (var u in battle.PlayerUnits)
                sb.AppendLine($"{u.Name}  HP {u.currentHP}/{u.MaxHP}  MP {u.currentMP}/{u.MaxMP}{(u.IsAlive ? "" : "  (KO)")}");
            partyStatusText.text = sb.ToString();
        }

        void ShowCommandMenu(BattleUnit actor)
        {
            currentActor = actor;
            ClearTargets();
            if (commandMenu) commandMenu.SetActive(true);
        }

        void OpenFirstSkill()
        {
            // Simplest possible skill selection: use the actor's first affordable skill.
            // Replace with a proper scrollable skill list in your UI pass.
            if (currentActor?.Skills == null) return;
            foreach (var s in currentActor.Skills)
            {
                if (s != null && s.mpCost <= currentActor.currentMP)
                {
                    bool targetsEnemy = s.target == SkillTarget.SingleEnemy || s.target == SkillTarget.AllEnemies;
                    if (s.target == SkillTarget.SingleEnemy || s.target == SkillTarget.SingleAlly)
                        BeginTargeting(BattleActionType.Skill, s, null);
                    else
                        Submit(BattleAction.Skill_(s, null));
                    return;
                }
            }
            AppendLog("No usable skills.");
        }

        void OpenFirstItem()
        {
            var gm = GameManager.Instance;
            foreach (var slot in gm.Inventory.Slots)
            {
                if (slot.item.usableInBattle && slot.item.type == ItemType.Consumable)
                {
                    // Default to using it on the current actor; expand to a target picker as needed.
                    Submit(BattleAction.Item_(slot.item, currentActor));
                    return;
                }
            }
            AppendLog("No usable items.");
        }

        void BeginTargeting(BattleActionType type, SkillData skill, ItemData item)
        {
            if (commandMenu) commandMenu.SetActive(false);
            ClearTargets();
            if (targetContainer == null || targetButtonPrefab == null)
            {
                // No target UI wired — auto-target the first living enemy.
                var t = battle.EnemyUnits.Find(u => u.IsAlive);
                Submit(Build(type, skill, item, t));
                return;
            }

            foreach (var enemy in battle.EnemyUnits)
            {
                if (!enemy.IsAlive) continue;
                var btn = Instantiate(targetButtonPrefab, targetContainer);
                var label = btn.GetComponentInChildren<TMP_Text>();
                if (label) label.text = enemy.Name;
                var captured = enemy;
                btn.onClick.AddListener(() => Submit(Build(type, skill, item, captured)));
                targetButtons.Add(btn);
            }
        }

        BattleAction Build(BattleActionType type, SkillData skill, ItemData item, BattleUnit target) => type switch
        {
            BattleActionType.Attack => BattleAction.Attack(target),
            BattleActionType.Skill => BattleAction.Skill_(skill, target),
            BattleActionType.Item => BattleAction.Item_(item, target),
            _ => BattleAction.Attack(target),
        };

        void Submit(BattleAction action)
        {
            if (commandMenu) commandMenu.SetActive(false);
            ClearTargets();
            battle.SubmitPlayerAction(action);
        }

        void HandleFinished(bool victory)
        {
            if (commandMenu) commandMenu.SetActive(false);
            ClearTargets();
            if (root) root.SetActive(false);
        }

        void ClearTargets()
        {
            foreach (var b in targetButtons) if (b) Destroy(b.gameObject);
            targetButtons.Clear();
        }
    }
}
