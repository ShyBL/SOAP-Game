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
    public abstract class QuestObjectiveSO : ScriptableObject, ISOCreateNamingContext, ISOCreateVariableNamingContext
    {
        [Header("Trigger")]
        [Tooltip("The content-specific event ID that advances this objective when " +
                 "raised on the QuestChannel — e.g. 'survivor_rescued'. Free-form, " +
                 "not a GameTopic; matched against whatever trigger scripts raise.")]
        [EventIdField]
        public string EventId;

        [Tooltip("Shown in the HUD. E.g. 'Rescue survivors', 'Defeat Malgor'.")]
        public string DisplayLabel;

        [Header("Persistence")]
        [Tooltip("Stable identity used as part of this objective's save key. " +
                 "Auto-filled with a GUID if left empty — safe to leave alone. " +
                 "A future centralized quest-authoring tool may manage this differently.")]
        public string ObjectiveId;
        
        [Header("Ownership")]
        [Tooltip("Set automatically by QuestAuthoringWindow when this objective is created. " +
                 "Used only for naming nested [SOCreate] fields (e.g. Counter/Flag) — not read at runtime.")]
        [HideInInspector]
        public string QuestId;
        
        /// <summary>
        /// Called by QuestManager when EventId fires and the owning quest
        /// is active and incomplete. Mutates the underlying SOAP variable.
        /// </summary>
        public abstract void OnEventFired();

        /// <summary>
        /// True when this objective's completion condition is satisfied.
        /// Evaluated by QuestManager after every OnEventFired call.
        /// </summary>
        public abstract bool IsComplete { get; }

        /// <summary>
        /// Human-readable progress string for generic UI display (e.g. "3 / 6"
        /// for a CountUpObjectiveSO, "Complete" for a BoolFlagObjectiveSO).
        /// </summary>
        public abstract string ProgressLabel { get; }

        /// <summary>
        /// Current progress serialized as a string, for QuestManager to persist
        /// via SaveSystem.
        /// </summary>
        public abstract string SaveValue { get; }

        /// <summary>
        /// Restores progress from a previously saved string (see SaveValue).
        /// Must fail safely — a malformed or unexpected value should leave the
        /// underlying variable at its current value rather than throw.
        /// </summary>
        public abstract void LoadValue(string savedValue);

        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(EventId))
                Debug.LogWarning($"[QuestObjectiveSO] '{name}': EventId is empty — this objective will never advance.", this);

            if (string.IsNullOrEmpty(ObjectiveId))
                ObjectiveId = System.Guid.NewGuid().ToString();
        }

        public string NamingPrefix => QuestId;

        public string VariableShortName =>
            string.IsNullOrEmpty(DisplayLabel) ? name : DisplayLabel.Replace(" ", "");
    }
}
