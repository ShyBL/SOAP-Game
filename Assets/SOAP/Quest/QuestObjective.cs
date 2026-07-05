using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Abstract base for all quest objectives. One asset per objective.
    /// QuestDefinitionSO holds an array of these.
    ///
    /// Concrete subtypes own only the fields they need and implement their
    /// own logic — no discriminator enum, no dead fields, no factory.
    ///
    ///   CountUpObjectiveSO  — IntVariable incremented toward a target
    ///   BoolFlagObjectiveSO — BoolVariable set to true once
    ///
    /// To add a new objective type: create one new class that extends this.
    /// QuestManager, QuestDefinitionSO, and all existing objectives are untouched.
    /// </summary>
    public abstract class QuestObjective : ScriptableObject
    {
        [Header("Trigger")]
        [Tooltip("The GameTopic that advances this objective when raised on the QuestChannel.")]
        public GameTopic TopicTrigger;

        [Tooltip("Shown in the HUD. E.g. 'Rescue survivors', 'Defeat Malgor'.")]
        public string DisplayLabel;

        /// <summary>
        /// Called by QuestManager when TopicTrigger fires and the owning quest
        /// is active and incomplete. Mutates the underlying SOAP variable.
        /// </summary>
        public abstract void OnEventFired();

        /// <summary>
        /// True when this objective's completion condition is satisfied.
        /// Evaluated by QuestManager after every OnEventFired call.
        /// </summary>
        public abstract bool IsComplete { get; }

        protected virtual void OnValidate()
        {
            if (TopicTrigger == GameTopic.None)
                Debug.LogWarning($"[QuestObjectiveSO] '{name}': TopicTrigger is None — this objective will never advance.", this);
        }
    }
}