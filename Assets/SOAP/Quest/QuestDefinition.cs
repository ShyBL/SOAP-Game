using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Defines a complete quest. One asset per quest.
    ///
    /// Holds an array of QuestObjectiveSO references — mix and match concrete
    /// subtypes freely (CountUpObjectiveSO, BoolFlagObjectiveSO, or any future
    /// type). Unity serialises subtype references correctly in an abstract base
    /// array, so no casting or type-checking is needed here or in QuestManager.
    ///
    /// Inspector setup for MQ_001:
    ///   QuestId     = "MQ_001"
    ///   DisplayName = "The Fall of Raven Hollow"
    ///   Objectives  = [ MQ_001_SurvivorsRescued (CountUp),
    ///                   MQ_001_EnemiesKilled     (CountUp),
    ///                   MQ_001_EchoesFound       (CountUp),
    ///                   MQ_001_HeartCollected    (BoolFlag),
    ///                   MQ_001_BossDefeated      (BoolFlag),
    ///                   MQ_001_PortalSealed      (BoolFlag) ]
    ///   QuestActive   → QuestActive_Bool
    ///   QuestComplete → QuestComplete_Bool
    /// </summary>
    [CreateAssetMenu(fileName = "New_QuestDefinition", menuName = "SOAP/Quest/Definition")]
    public class QuestDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string QuestId;
        public string DisplayName;
 
        [Header("Objectives")]
        [Tooltip("All objectives that must be complete for this quest to be considered finished. " +
                 "Order does not matter — all are checked simultaneously.")]
        public QuestObjective[] Objectives;
 
        [Header("State")]
        [Tooltip("Set to true externally (e.g. by a quest giver) to allow QuestManager " +
                 "to begin routing events to this quest's objectives.")]
        public BoolVariable QuestActive;
 
        [Tooltip("Set to true by QuestManager when all objectives are complete. " +
                 "Triggers QuestCompleted on the channel.")]
        public BoolVariable QuestComplete;
 
        [Tooltip("Set true by QuestTurnInZone when the player returns to the NPC. " +
                 "Prevents the quest being handed in a second time. " +
                 "Create a dedicated BoolVariable SO per quest: e.g. MQ_001_TurnedIn_Bool.")]
        public BoolVariable TurnedIn;
        
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(QuestId))
                Debug.LogWarning($"[QuestDefinitionSO] '{name}': QuestId is empty.", this);
            if (QuestActive == null)
                Debug.LogWarning($"[QuestDefinitionSO] '{name}': QuestActive BoolVariable is not assigned.", this);
            if (QuestComplete == null)
                Debug.LogWarning($"[QuestDefinitionSO] '{name}': QuestComplete BoolVariable is not assigned.", this);
            if (TurnedIn == null)
                Debug.LogWarning($"[QuestDefinitionSO] '{name}': TurnedIn BoolVariable is not assigned. " +
                                 "Create a MQ_XXX_TurnedIn_Bool asset and wire it here.", this);
        }
    }
}