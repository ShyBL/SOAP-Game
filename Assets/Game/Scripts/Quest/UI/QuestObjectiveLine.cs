using TMPro;
using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// One bulleted line inside a QuestJournalEntry, representing a single
    /// objective's DisplayLabel and live ProgressLabel. The bullet icon is a
    /// static child image on this prefab — this component only drives the
    /// text, so it never needs to know which concrete objective type it's
    /// displaying.
    ///
    /// Inspector setup (on the line prefab):
    ///   labelText → TMP element showing "{DisplayLabel}: {ProgressLabel}",
    ///               struck through when the step is complete
    /// </summary>
    public class QuestObjectiveLine : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI labelText;

        private void OnValidate()
        {
            if (labelText == null)
                Debug.LogError($"[QuestObjectiveLine] LabelText is not assigned on {name}!", this);
        }

        public void Populate(QuestObjectiveSO objective, QuestDefinitionSO.StepState state)
        {
            if (objective == null) return;

            labelText.text = state == QuestDefinitionSO.StepState.Completed
                ? $"<s>{objective.DisplayLabel}</s>"
                : $"{objective.DisplayLabel}: {objective.ProgressLabel}";
        }
    }
}