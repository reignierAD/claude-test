using System.Collections.Generic;
using UnityEngine;
using SuikodenLike.Core;
using SuikodenLike.Data;

namespace SuikodenLike.Field
{
    /// <summary>
    /// Put this on the player alongside PlayerController. While inside an
    /// EncounterZone it accumulates walked distance and, once a randomized
    /// threshold is passed, rolls an enemy group and asks the GameManager to
    /// start a battle — the classic "step and get ambushed" mechanic.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class RandomEncounterController : MonoBehaviour
    {
        [Tooltip("Grace distance after a battle before encounters can trigger again.")]
        [SerializeField] float postBattleSafeDistance = 3f;

        PlayerController player;
        EncounterTable activeTable;
        int zoneStack; // supports overlapping zones
        float distanceAccum;
        float nextThreshold;
        float safeRemaining;

        void Awake() => player = GetComponent<PlayerController>();

        // Subscribe in Start, not OnEnable: GameManager.Instance is assigned in
        // Awake, and OnEnable can run before it on the very first frame.
        void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnBattleEnded += HandleBattleEnded;
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnBattleEnded -= HandleBattleEnded;
        }

        public void EnterZone(EncounterZone zone)
        {
            zoneStack++;
            activeTable = zone.Table;
            RollNextThreshold();
        }

        public void ExitZone(EncounterZone zone)
        {
            zoneStack = Mathf.Max(0, zoneStack - 1);
            if (zoneStack == 0)
            {
                activeTable = null;
                distanceAccum = 0f;
            }
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.InBattle || activeTable == null || !player.ControlEnabled)
                return;

            float walked = player.DistanceWalked;
            player.ResetDistanceWalked();

            if (safeRemaining > 0f)
            {
                safeRemaining -= walked;
                return;
            }

            distanceAccum += walked;
            if (distanceAccum >= nextThreshold)
                TriggerEncounter(gm);
        }

        void TriggerEncounter(GameManager gm)
        {
            distanceAccum = 0f;
            RollNextThreshold();

            var group = activeTable.RollGroup();
            if (group == null || group.enemies == null || group.enemies.Count == 0) return;

            gm.StartBattle(new List<EnemyData>(group.enemies));
        }

        void RollNextThreshold()
        {
            if (activeTable == null) { nextThreshold = float.MaxValue; return; }
            float baseSteps = activeTable.averageStepsBetweenEncounters;
            float v = activeTable.variance;
            nextThreshold = baseSteps * (1f + Random.Range(-v, v));
            nextThreshold = Mathf.Max(0.5f, nextThreshold);
        }

        void HandleBattleEnded(bool playerWon)
        {
            safeRemaining = postBattleSafeDistance;
            distanceAccum = 0f;
            RollNextThreshold();
        }
    }
}
