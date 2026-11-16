using System.Text;

namespace SOAP
{
    /// <summary>
    /// Formats a quest's reward preview text from its authored data. Shared
    /// by QuestOfferPanel and QuestTurnInPanel so both flows always agree on
    /// how rewards are displayed — single source of truth for formatting,
    /// same as RewardXP/RewardGold being the single source of truth for the
    /// values themselves.
    /// </summary>
    public static class QuestRewardFormatter
    {
        public static string FormatRewardPreview(QuestDefinitionSO quest)
        {
            var sb = new StringBuilder();
            sb.Append($"{quest.RewardXP} XP   {quest.RewardGold} Gold");

            if (!string.IsNullOrEmpty(quest.RewardExtraText))
            {
                sb.AppendLine();
                sb.Append(quest.RewardExtraText);
            }

            return sb.ToString();
        }
    }
}
