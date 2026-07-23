using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuikodenLike.Data
{
    [Serializable]
    public class EncounterGroup
    {
        public string label = "Group";
        [Tooltip("Enemies that appear together in this battle.")]
        public List<EnemyData> enemies = new List<EnemyData>();
        [Tooltip("Relative weight vs other groups in this table.")]
        [Min(0f)] public float weight = 1f;
    }

    /// <summary>
    /// A zone's random-encounter definition. Assign one to each
    /// EncounterZone in the field. Controls which enemy groups appear
    /// and how often steps trigger a fight.
    /// Create via: Assets > Create > SuikodenLike > Encounter Table.
    /// </summary>
    [CreateAssetMenu(fileName = "New Encounter Table", menuName = "SuikodenLike/Encounter Table")]
    public class EncounterTable : ScriptableObject
    {
        [Tooltip("Average distance (world units) walked before a fight can trigger.")]
        [Min(0.1f)] public float averageStepsBetweenEncounters = 8f;

        [Tooltip("Random +/- variance applied to the step counter each time.")]
        [Range(0f, 1f)] public float variance = 0.5f;

        public List<EncounterGroup> groups = new List<EncounterGroup>();

        /// <summary>Weighted pick of one enemy group.</summary>
        public EncounterGroup RollGroup()
        {
            if (groups == null || groups.Count == 0) return null;
            float total = 0f;
            foreach (var g in groups) total += Mathf.Max(0f, g.weight);
            if (total <= 0f) return groups[0];

            float roll = UnityEngine.Random.value * total;
            foreach (var g in groups)
            {
                roll -= Mathf.Max(0f, g.weight);
                if (roll <= 0f) return g;
            }
            return groups[groups.Count - 1];
        }
    }
}
