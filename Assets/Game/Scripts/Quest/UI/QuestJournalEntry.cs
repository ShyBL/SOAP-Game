using TMPro;
using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// One row in the quest journal — one instance per concurrently tracked
    /// quest, created/destroyed by QuestJournalUI as quests are accepted and
    /// completed.
    ///
    /// Locked steps (not yet reached, per QuestDefinitionSO.GetStepState) are
    /// omitted entirely rather than shown as "???" — see quest-step-ordering
    /// design notes.
    ///
    /// Inspector setup (on the entry prefab):
    ///   titleText           → TMP element for DisplayName
    ///   objectivesContainer → empty Transform that objective line instances
    ///                          get parented under (e.g. a Vertical Layout
    ///                          Group so lines stack automatically)
    ///   objectiveLinePrefab → a QuestObjectiveLine prefab (bullet image +
    ///                          one line of text), instantiated once per
    ///                          visible objective
    /// </summary>
    public class QuestJournalEntry : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Transform objectivesContainer;
        [SerializeField] private QuestObjectiveLine objectiveLinePrefab;

        private void OnValidate()
        {
            if (titleText == null)
                Debug.LogError($"[QuestJournalEntry] TitleText is not assigned on {name}!", this);
            if (objectivesContainer == null)
                Debug.LogError($"[QuestJournalEntry] ObjectivesContainer is not assigned on {name}!", this);
            if (objectiveLinePrefab == null)
                Debug.LogError($"[QuestJournalEntry] ObjectiveLinePrefab is not assigned on {name}!", this);
        }

        /// <summary>
        /// Fills the row from the quest's current state. Called on creation
        /// and again on every refresh. Rebuilds the objective lines from
        /// scratch each call — simple and always correct, and cheap enough
        /// since this only runs on discrete quest/objective events rather
        /// than per-frame.
        /// </summary>
        public void Populate(QuestDefinitionSO quest)
        {
            titleText.text = quest.DisplayName;

            for (int i = objectivesContainer.childCount - 1; i >= 0; i--)
                Destroy(objectivesContainer.GetChild(i).gameObject);

            if (quest.Objectives == null) return;

            for (int i = 0; i < quest.Objectives.Length; i++)
            {
                var objective = quest.Objectives[i];
                if (objective == null) continue;

                var state = quest.GetStepState(i);
                if (state == QuestDefinitionSO.StepState.Locked) continue;

                var line = Instantiate(objectiveLinePrefab, objectivesContainer);
                line.Populate(objective, state);
            }
        }
    }
}