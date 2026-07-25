using System.Collections.Generic;
using UnityEngine;
using SuikodenLike.Core;
using SuikodenLike.Data;
using SuikodenLike.Field;

namespace SuikodenLike.Battle
{
    /// <summary>
    /// The visual half of a battle. The arena lives far away in world space
    /// (so it never overlaps the map); when a fight starts this parks the
    /// camera there, spawns a sprite per combatant, and freezes the player.
    /// When the fight ends it tidies up and hands the camera back.
    /// </summary>
    public class BattleSceneView : MonoBehaviour
    {
        [SerializeField] BattleManager battle;
        [SerializeField] Transform arenaRoot;
        [SerializeField] Camera cam;
        [SerializeField] CameraFollow cameraFollow;
        [SerializeField] PlayerController player;

        [Header("Layout (local to the arena)")]
        [SerializeField] float enemyRowY = 2.0f;
        [SerializeField] float partyRowY = -3.0f;
        [SerializeField] float spacing = 3.2f;
        [SerializeField] int sortingOrder = 10;

        [Header("On defeat")]
        [Tooltip("Where the party wakes up after a wipe. Empty = stay put.")]
        [SerializeField] Transform respawnPoint;
        [Tooltip("Revive the party to full HP after a defeat instead of ending the run.")]
        [SerializeField] bool reviveOnDefeat = true;

        readonly List<GameObject> spawned = new List<GameObject>();

        void Start()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnBattleRequested += HandleBattleRequested;
                gm.OnBattleEnded += HandleBattleEnded;
            }
        }

        void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnBattleRequested -= HandleBattleRequested;
                gm.OnBattleEnded -= HandleBattleEnded;
            }
        }

        void HandleBattleRequested(List<EnemyData> enemies)
        {
            if (player != null) player.ControlEnabled = false;

            // Park the camera on the arena.
            if (cameraFollow != null) cameraFollow.enabled = false;
            if (cam != null && arenaRoot != null)
            {
                Vector3 p = arenaRoot.position;
                cam.transform.position = new Vector3(p.x, p.y, cam.transform.position.z);
            }

            SpawnEnemies(enemies);
            SpawnParty();
        }

        void HandleBattleEnded(bool playerWon)
        {
            ClearSpawned();

            // A wipe would otherwise drop the player back on the map at 0 HP,
            // straight into the next encounter. Pick them up and move them.
            // Note this checks for an actual wipe, not just !playerWon —
            // running away from a fight is also "not a win".
            var gm = GameManager.Instance;
            if (reviveOnDefeat && gm != null && gm.IsPartyWiped())
            {
                foreach (var m in gm.Party) m.RecomputeAndFullHeal();
                if (respawnPoint != null && player != null)
                    player.transform.position = respawnPoint.position;
            }

            if (cameraFollow != null)
            {
                cameraFollow.enabled = true;
                cameraFollow.SnapToTarget();
            }
            if (player != null) player.ControlEnabled = true;
        }

        void SpawnEnemies(List<EnemyData> enemies)
        {
            if (enemies == null) return;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null) continue;
                Vector3 pos = SlotPosition(i, enemies.Count, enemyRowY);
                Spawn(e.displayName, e.sprite, pos, false);
            }
        }

        void SpawnParty()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            var party = gm.Party;
            for (int i = 0; i < party.Count; i++)
            {
                var m = party[i];
                if (m == null || m.data == null) continue;
                Vector3 pos = SlotPosition(i, party.Count, partyRowY);
                Spawn(m.DisplayName, m.data.sprite, pos, true);
            }
        }

        Vector3 SlotPosition(int index, int count, float rowY)
        {
            float width = (count - 1) * spacing;
            float x = -width * 0.5f + index * spacing;
            // Stagger every other slot so overlapping sprites stay readable.
            float y = rowY + (index % 2 == 1 ? -0.6f : 0f);
            Vector3 origin = arenaRoot != null ? arenaRoot.position : Vector3.zero;
            return origin + new Vector3(x, y, 0f);
        }

        void Spawn(string label, Sprite sprite, Vector3 position, bool isParty)
        {
            if (sprite == null) return;
            var go = new GameObject((isParty ? "Ally_" : "Enemy_") + label);
            go.transform.SetParent(arenaRoot != null ? arenaRoot : transform, true);
            go.transform.position = position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            // Party sprites face the enemies across the field.
            if (isParty) go.transform.localScale = new Vector3(-1f, 1f, 1f);

            spawned.Add(go);
        }

        void ClearSpawned()
        {
            foreach (var go in spawned) if (go != null) Destroy(go);
            spawned.Clear();
        }
    }
}
