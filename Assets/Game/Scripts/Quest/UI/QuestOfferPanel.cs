using System.Text;
using TMPro;
using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Displays a quest offer: name, flavor text, objective list, and reward
    /// preview. Populated by QuestOfferUIController — this component only
    /// renders, it holds no quest-tracking state of its own.
    ///
    /// Inspector setup:
    ///   titleText       → TMP element for DisplayName
    ///   descriptionText → TMP element for Description
    ///   objectivesText  → TMP element listing each objective's DisplayLabel,
    ///                      one per line
    ///   rewardText      → TMP element formatted from RewardXP, RewardGold,
    ///                      and RewardExtraText (via QuestRewardFormatter)
    ///
    /// Accept/Decline buttons are wired directly to QuestOfferUIController's
    /// Accept() and Decline() methods in the inspector — this panel doesn't
    /// need button references itself.
    /// </summary>
    public class QuestOfferPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI objectivesText;
        [SerializeField] private TextMeshProUGUI rewardText;

        private void OnValidate()
        {
            if (titleText == null)
                Debug.LogError($"[QuestOfferPanel] TitleText is not assigned on {name}!", this);
            if (descriptionText == null)
                Debug.LogError($"[QuestOfferPanel] DescriptionText is not assigned on {name}!", this);
            if (objectivesText == null)
                Debug.LogError($"[QuestOfferPanel] ObjectivesText is not assigned on {name}!", this);
            if (rewardText == null)
                Debug.LogError($"[QuestOfferPanel] RewardText is not assigned on {name}!", this);
        }

        /// <summary>
        /// Fills every field from the quest's authored data. Safe to call
        /// with any QuestDefinitionSO — reads Objectives generically via
        /// DisplayLabel, no knowledge of concrete objective types needed.
        /// </summary>
        public void Populate(QuestDefinitionSO quest)
        {
            titleText.text = quest.DisplayName;
            descriptionText.text = quest.Description;
            objectivesText.text = BuildObjectivesList(quest);
            rewardText.text = QuestRewardFormatter.FormatRewardPreview(quest);
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        private static string BuildObjectivesList(QuestDefinitionSO quest)
        {
            if (quest.Objectives == null || quest.Objectives.Length == 0)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var objective in quest.Objectives)
            {
                if (objective == null) continue;
                sb.AppendLine($"• {objective.DisplayLabel}");
            }
            return sb.ToString().TrimEnd();
        }
    }
}
