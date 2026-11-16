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
    /// Objectives are completed in array order — see CurrentObjective/GetStepState.
    ///
    /// Inspector setup for MQ_001:
    ///   QuestId         = "MQ_001"
    ///   DisplayName     = "The Fall of Raven Hollow"
    ///   Description     = "Three days ago a portal opened in Raven Hollow.
    ///                       Since then, the city has fallen. Survivors remain
    ///                       inside. The gate must be closed."
    ///   Objectives      = [ MQ_001_SurvivorsRescued (CountUp),
    ///                       MQ_001_EnemiesKilled     (CountUp),
    ///                       MQ_001_EchoesFound       (CountUp),
    ///                       MQ_001_HeartCollected    (BoolFlag),
    ///                       MQ_001_BossDefeated      (BoolFlag),
    ///                       MQ_001_PortalSealed      (BoolFlag) ]
    ///   QuestActive     → QuestActive_Bool
    ///   QuestComplete   → QuestComplete_Bool
    ///   TurnedIn        → QuestTurnedIn_Bool (dedicated per-quest asset, prevents
    ///                       a completed quest from being handed in twice)
    ///   Prerequisites   → quests that must be QuestComplete before this one is offerable
    ///                       (e.g. MQ_001's Prologue scroll quest)
    ///   RewardXP        = 2500
    ///   RewardGold      = 500
    ///   RewardExtraText = "Random Legendary Gear Piece. Story unlock:
    ///                       4 additional gates revealed."
    /// </summary>
    [CreateAssetMenu(fileName = "New_QuestDefinition", menuName = "SOAP/Quest/Definition")]
    public class QuestDefinitionSO : ScriptableObject, ISOCreateNamingContext
    {
        [Header("Identity")]
        public string QuestId;
        public string DisplayName;

        [Header("Presentation")]
        [Tooltip("Quest-giver's opening line / flavor text, shown on the offer panel.")]
        [TextArea(3, 6)]
        public string Description;

        [Header("Objectives")]
        [Tooltip("Objectives are completed in array order. The first incomplete objective is the " +
                 "quest's current step; later ones are locked until it clears. Also doubles as the " +
                 "requirements list shown on the offer panel (via each objective's DisplayLabel).")]
        [SOCreate]
        public QuestObjectiveSO[] Objectives;

        /// <summary>Step state for a single index in Objectives, for UI presentation.</summary>
        public enum StepState { Completed, Current, Locked }

        /// <summary>
        /// The first incomplete objective in array order, or null if every
        /// objective is complete. QuestManager only routes events to this
        /// objective — see the CurrentObjective check in Dispatch.
        /// </summary>
        public QuestObjectiveSO CurrentObjective
        {
            get
            {
                if (Objectives == null) return null;
                foreach (var objective in Objectives)
                    if (objective != null && !objective.IsComplete)
                        return objective;
                return null;
            }
        }

        /// <summary>Completed / Current / Locked for Objectives[index], for journal/UI presentation.</summary>
        public StepState GetStepState(int index)
        {
            var objective = Objectives[index];
            if (objective == null || objective.IsComplete) return StepState.Completed;
            return objective == CurrentObjective ? StepState.Current : StepState.Locked;
        }

        [Header("Rewards")]
        [Tooltip("XP granted on completion. Single source of truth for both the offer panel " +
                 "preview and the actual payout in RewardDispenser.")]
        public int RewardXP;

        [Tooltip("Gold granted on completion. Single source of truth for both the offer panel " +
                 "preview and the actual payout in RewardDispenser.")]
        public int RewardGold;

        [Tooltip("Free-text description of non-numeric rewards (items, unlocks). Display-only — " +
                 "any mechanical effect (item grants, flag unlocks) is wired separately on " +
                 "RewardDispenser's onRewardsGiven UnityEvent.")]
        [TextArea(1, 3)]
        public string RewardExtraText;

        [Tooltip("Items granted when the quest is turned in. Author them here and QuestRewardGranter " +
                 "pays them out automatically — no per-quest scene wiring. Anything that does not " +
                 "fit in the backpack drops as a world pickup.")]
        public ItemStack[] RewardItems;

        [Header("State")]
        [Tooltip("Set to true externally (e.g. by a quest giver, or QuestGrantPickup for a quest " +
                 "with no giver) to allow QuestManager to begin routing events to this quest's " +
                 "objectives.")]
        [SOCreate]
        public BoolVariable QuestActive;

        [Tooltip("Set to true by QuestManager when all objectives are complete. " +
                 "Triggers QuestCompleted on the channel.")]
        [SOCreate]
        public BoolVariable QuestComplete;

        [Tooltip("Set to true by QuestTurnInZone when the player returns to the NPC. " +
                 "Prevents the quest being handed in a second time. " +
                 "Create a dedicated BoolVariable SO per quest: e.g. MQ_001_TurnedIn_Bool.")]
        [SOCreate]
        public BoolVariable QuestTurnedIn;

        [Header("Prerequisites")]
        [Tooltip("Not offerable until every quest here has QuestComplete == true. Empty = no prerequisite.")]
        public QuestDefinitionSO[] Prerequisites;

        /// <summary>True if every entry in Prerequisites is complete (or Prerequisites is empty).</summary>
        public bool PrerequisitesMet()
        {
            if (Prerequisites == null) return true;
            foreach (var prereq in Prerequisites)
            {
                if (prereq == null) continue;
                if (prereq.QuestComplete == null || !prereq.QuestComplete.currentValue) return false;
            }
            return true;
        }

        public string NamingPrefix { get; }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(QuestId))
                Debug.LogWarning($"[QuestDefinitionSO] '{name}': QuestId is empty.", this);
            if (QuestComplete == null)
                Debug.LogWarning($"[QuestDefinitionSO] '{name}': QuestComplete BoolVariable is not assigned.", this);
            if (QuestTurnedIn == null)
                Debug.LogWarning($"[QuestDefinitionSO] '{name}': QuestTurnedIn  BoolVariable is not assigned. " +
                                 "Create a MQ_XXX_TurnedIn_Bool asset and wire it here.", this);

            if (RewardItems != null)
                for (int i = 0; i < RewardItems.Length; i++)
                    if (RewardItems[i].Item != null && RewardItems[i].Count < 1)
                        RewardItems[i].Count = 1;
        }
    }
}