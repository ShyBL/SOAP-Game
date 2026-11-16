using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Optional HUD helper. Drives a SetTextFromFloat label with a live
    /// "current / target" counter whenever a quest objective changes.
    ///
    /// Can be skipped entirely if the HUD is wired directly to the IntVariables
    /// via their own TopicResponder + SetTextFromFloat chain.
    ///
    /// Inspector setup:
    ///   counter  → the IntVariable to display (e.g. SurvivorsRescued_Int)
    ///   target   → the objective threshold     (e.g. 6)
    ///   hudLabel → the SetTextFromFloat on the HUD text element
    ///              Configure the SetTextFromFloat as follows:
    ///                prefix         → e.g. "Survivors: "
    ///                suffix         → " / "
    ///                targetVariable → any FloatVariable with maxValue = [target]
    ///                                 (create a dedicated display-only asset)
    ///
    /// Channel wiring (no code required):
    ///   Add an IntTopicResponder to this same GameObject.
    ///   Set channel = QuestChannel, topicToListenFor = QuestObjectiveUpdated.
    ///   Wire onResponseTriggered → ObjectiveTracker.Refresh().
    ///
    /// One ObjectiveTracker per counter:
    ///   Add three instances (survivors, enemies, echoes). Wire each to its
    ///   own IntVariable and target value.
    /// </summary>
    public class ObjectiveTracker : MonoBehaviour
    {
        [SerializeField] private IntVariable counter;
        [SerializeField] private int target;
        [SerializeField] private SetTextFromFloat hudLabel;

        private void OnValidate()
        {
            if (counter == null)
                Debug.LogError($"[ObjectiveTracker] Counter IntVariable is not assigned on {name}!", this);
            if (hudLabel == null)
                Debug.LogError($"[ObjectiveTracker] HudLabel SetTextFromFloat is not assigned on {name}!", this);
        }

        /// <summary>
        /// Called by the IntTopicResponder's onResponseTriggered when
        /// QuestObjectiveUpdated fires. Pushes the current count to the HUD.
        /// </summary>
        public void Refresh()
        {
            // int → float implicit cast; SetTextFromFloat formats as "{prefix}{value}{suffix}{maxValue}"
            // Configure the linked SetTextFromFloat's targetVariable.maxValue to match [target].
            hudLabel.SetText(counter.currentValue);
        }
    }
}
