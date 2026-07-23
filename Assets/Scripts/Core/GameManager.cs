using System;
using System.Collections.Generic;
using UnityEngine;
using SuikodenLike.Data;
using SuikodenLike.Party;
using SuikodenLike.Inventory;

namespace SuikodenLike.Core
{
    /// <summary>
    /// Central persistent hub. Survives scene loads, owns the party,
    /// the inventory and story flags, and brokers transitions between the
    /// field and battle. One instance in the boot scene; everything else
    /// reads GameManager.Instance.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("New Game setup")]
        [Tooltip("Characters the player starts with.")]
        [SerializeField] List<CharacterData> startingParty = new List<CharacterData>();
        [SerializeField] int startingGold = 100;
        [Tooltip("Items the player starts with.")]
        [SerializeField] List<ItemData> startingItems = new List<ItemData>();

        public List<PartyMember> Party { get; private set; } = new List<PartyMember>();
        public Inventory.Inventory Inventory { get; private set; } = new Inventory.Inventory();

        readonly HashSet<string> flags = new HashSet<string>();

        /// <summary>Raised when a field encounter/battle should begin.</summary>
        public event Action<List<EnemyData>> OnBattleRequested;
        /// <summary>Raised when battle ends. bool = player victory.</summary>
        public event Action<bool> OnBattleEnded;

        public bool InBattle { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            NewGame();
        }

        public void NewGame()
        {
            Party.Clear();
            foreach (var c in startingParty)
                if (c != null) Party.Add(new PartyMember(c));

            Inventory = new Inventory.Inventory();
            Inventory.AddGold(startingGold);
            foreach (var it in startingItems)
                if (it != null) Inventory.Add(it, 1);

            flags.Clear();
        }

        // --- Party helpers ---
        public void RecruitCharacter(CharacterData data)
        {
            if (data == null) return;
            if (Party.Exists(p => p.data == data)) return; // already recruited
            Party.Add(new PartyMember(data));
        }

        public bool IsPartyWiped()
        {
            foreach (var m in Party) if (m.IsAlive) return false;
            return true;
        }

        // --- Story flags ---
        public bool HasFlag(string flag) => !string.IsNullOrEmpty(flag) && flags.Contains(flag);
        public void SetFlag(string flag) { if (!string.IsNullOrEmpty(flag)) flags.Add(flag); }
        public void ClearFlag(string flag) => flags.Remove(flag);

        // --- Battle brokering (called by the field encounter system) ---
        public void StartBattle(List<EnemyData> enemies)
        {
            if (InBattle || enemies == null || enemies.Count == 0) return;
            InBattle = true;
            OnBattleRequested?.Invoke(enemies);
        }

        /// <summary>Called by BattleManager when the fight resolves.</summary>
        public void EndBattle(bool playerWon)
        {
            InBattle = false;
            OnBattleEnded?.Invoke(playerWon);
        }
    }
}
