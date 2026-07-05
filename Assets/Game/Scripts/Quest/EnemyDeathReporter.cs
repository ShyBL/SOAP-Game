using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Place on every enemy prefab, including Malgor and the two Infernal Harbingers.
    /// Called by the health or death system when the enemy dies.
    ///
    /// Inspector setup:
    ///   questChannel → QuestChannel SO
    ///   isBoss       → false for standard enemies; true for Malgor only
    ///
    /// isBoss = false → raises EnemyKilled   (heard by QuestManager, counts toward 10)
    /// isBoss = true  → raises BossDefeated  (heard by QuestManager, gates completion)
    ///
    /// Wire your health/death system to call ReportDeath() via UnityEvent or
    /// a direct method call.
    /// </summary>
    public class EnemyDeathReporter : MonoBehaviour
    {
        [SerializeField] private GameChannel questChannel;
        [SerializeField] private bool isBoss = false;
 
        private void OnValidate()
        {
            if (questChannel == null)
                Debug.LogError($"[EnemyDeathReporter] QuestChannel is not assigned on {name}!", this);
        }
 
        /// <summary>
        /// Call this from the health system when the enemy's HP reaches zero.
        /// </summary>
        public void ReportDeath()
        {
            GameTopic topic = isBoss ? GameTopic.BossDefeated : GameTopic.EnemyKilled;
            questChannel.Raise(topic);
        }
    }
}