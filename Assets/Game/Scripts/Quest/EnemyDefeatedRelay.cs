using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// The single hook between an enemy's health/death system and everything
    /// that reacts to its death. Wire the enemy's death event to OnDefeated()
    /// ONCE on the enemy prefab; after that the World Authoring window fills
    /// in the loot table and quest event for each placed enemy.
    ///
    /// On defeat: drops loot (while the enemy still has a position), then
    /// reports the kill so quest objectives advance. Fires once per life — it
    /// re-arms in OnEnable for pooled enemies.
    ///
    /// The LootDropper and EnemyDeathReporter on the same object are found
    /// automatically; the authoring window adds them if the prefab lacks them.
    /// </summary>
    public class EnemyDefeatedRelay : MonoBehaviour
    {
        private LootDropper lootDropper;
        private EnemyDeathReporter deathReporter;
        private bool defeated;

        private void Awake()
        {
            TryGetComponent(out lootDropper);
            TryGetComponent(out deathReporter);
        }

        private void OnEnable()
        {
            defeated = false;
        }

        /// <summary>Wire the enemy's death event to this.</summary>
        public void OnDefeated()
        {
            if (defeated) return;
            defeated = true;

            if (lootDropper != null)
                lootDropper.DropLoot();

            if (deathReporter != null)
                deathReporter.ReportDeath();
        }
    }
}
