using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// One asset per enemy type. The World Authoring window places enemies
    /// from this: it instantiates Prefab, adds the loot/quest components
    /// if missing, and wires LootTable and DeathEventId into them — nothing
    /// is configured by hand in the scene.
    ///
    ///   Prefab       → the enemy prefab (model, health, AI — whatever you have)
    ///   LootTable    → what it can drop (the window creates one per enemy)
    ///   DeathEventId → raised when it dies, e.g. "enemy_killed" for regular
    ///                  enemies or "malgor_defeated" for a specific boss. Must
    ///                  match a quest objective's EventId to advance anything.
    /// </summary>
    [CreateAssetMenu(fileName = "New_EnemyDefinition", menuName = "SOAP/World/Enemy Definition")]
    public class EnemyDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string EnemyId;
        public string DisplayName;

        [Header("Scene")]
        public GameObject Prefab;

        [Header("Drops")]
        public LootTableSO LootTable;

        [Header("Quest")]
        [EventIdField]
        public string DeathEventId = "enemy_killed";

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(EnemyId))
                Debug.LogWarning($"[EnemyDefinitionSO] '{name}': EnemyId is empty.", this);
            if (Prefab == null)
                Debug.LogWarning($"[EnemyDefinitionSO] '{name}': Prefab is not assigned — it cannot be placed in a scene.", this);
        }
    }
}
