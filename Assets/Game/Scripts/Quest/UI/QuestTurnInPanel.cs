using TMPro;
using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Lightweight "quest complete" display shown when the player turns in a
    /// finished quest. Shares reward formatting with QuestOfferPanel via
    /// QuestRewardFormatter so the numbers always match what was promised
    /// on the offer panel.
    ///
    /// Inspector setup:
    ///   titleText  → TMP element, e.g. "Quest Complete: {DisplayName}"
    ///   rewardText → TMP element formatted from RewardXP/RewardGold/RewardExtraText
    ///
    /// Continue button is wired directly to QuestTurnInUIController.Continue()
    /// in the inspector — this panel doesn't need a button reference itself.
    /// </summary>
    public class QuestTurnInPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI rewardText;

        private void OnValidate()
        {
            if (titleText == null)
                Debug.LogError($"[QuestTurnInPanel] TitleText is not assigned on {name}!", this);
            if (rewardText == null)
                Debug.LogError($"[QuestTurnInPanel] RewardText is not assigned on {name}!", this);
        }

        public void Populate(QuestDefinitionSO quest)
        {
            titleText.text = $"Quest Complete: {quest.DisplayName}";
            rewardText.text = QuestRewardFormatter.FormatRewardPreview(quest);
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
