using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Place on every enemy prefab.
    ///
    /// Inspector setup:
    ///   questChannel → QuestChannel SO
    ///   eventId      → e.g. "enemy_killed" for regular enemies, or
    ///                  "malgor_defeated" for a specific boss instance —
    ///                  one string per instance, no isBoss branch needed.
    ///                  Must match a QuestObjectiveSO's EventId to advance
    ///                  anything.
    ///
    /// Wire your health/death system to call ReportDeath() via UnityEvent or
    /// a direct method call.
    /// </summary>
    public class EnemyDeathReporter : MonoBehaviour
    {
        [SerializeField] private GameChannel questChannel;
        [SerializeField, EventIdField] private string eventId;

        private void OnValidate()
        {
            if (questChannel == null)
                Debug.LogError($"[EnemyDeathReporter] QuestChannel is not assigned on {name}!", this);
            if (string.IsNullOrEmpty(eventId))
                Debug.LogWarning($"[EnemyDeathReporter] EventId is empty on {name} — this won't advance anything.", this);
        }

        /// <summary>
        /// Call this from the health system when the enemy's HP reaches zero.
        /// </summary>
        public void ReportDeath()
        {
            questChannel.Raise(eventId);
        }
    }
}