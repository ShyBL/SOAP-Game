using UnityEngine;
using UnityEngine.Events;

namespace SOAP
{
    /// <summary>
    /// Grants XP and Gold when a quest completes, read generically from
    /// whichever QuestDefinitionSO just finished — no hardcoded amounts.
    ///
    /// Inspector setup:
    ///   Add a QuestDefinitionSOTopicResponder to this GameObject.
    ///     channel = QuestChannel, topicToListenFor = QuestCompleted
    ///     onTypedResponse → RewardDispenser.GiveRewards
    ///
    ///   playerXP               → PlayerXP_SavedInt SO     (saveKey: "player_xp")
    ///   playerGold             → PlayerGold_SavedInt SO   (saveKey: "player_gold")
    ///   additionalGatesRevealed→ optional. Only set if THIS dispenser instance
    ///                            grants a specific story unlock — not every
    ///                            quest needs one, so leave unassigned if unused.
    ///   onRewardsGiven         → wire gear drop logic, return-to-captain cinematic, etc.
    ///
    /// Rewards granted by GiveRewards(quest):
    ///   quest.RewardXP    XP    (persisted) — read from the quest, not hardcoded
    ///   quest.RewardGold  Gold  (persisted) — read from the quest, not hardcoded
    ///   additionalGatesRevealed = true, only if assigned
    ///   onRewardsGiven fires → handle gear drop and cinematics in the inspector
    ///
    /// Scene topology is unchanged from before this generalization: still one
    /// RewardDispenser per quest-giving NPC (or one shared instance, if every
    /// quest it serves has no NPC-specific extras) — the fix is that the
    /// numeric amounts now come from the completed quest itself, so they can
    /// never drift from what the offer panel promised.
    /// </summary>
    public class RewardDispenser : MonoBehaviour
    {
        [SerializeField, SOCreate] private SavedIntVariable playerXP;
        [SerializeField, SOCreate] private SavedIntVariable playerGold;
        [SerializeField, SOCreate] private BoolVariable additionalGatesRevealed;

        [Space]
        public UnityEvent OnRewardsGiven;

        private void OnValidate()
        {
            if (playerXP == null)
                Debug.LogError($"[RewardDispenser] PlayerXP SavedIntVariable is not assigned on {name}!", this);
            if (playerGold == null)
                Debug.LogError($"[RewardDispenser] PlayerGold SavedIntVariable is not assigned on {name}!", this);
            // additionalGatesRevealed is intentionally optional — not every
            // quest grants a story unlock, so no warning if it's left null.
        }

        /// <summary>
        /// Wired to QuestDefinitionSOTopicResponder.onTypedResponse for
        /// GameTopic.QuestCompleted. Reads reward amounts from the quest that
        /// just completed rather than from hardcoded literals.
        /// </summary>
        public void GiveRewards(QuestDefinitionSO quest)
        {
            if (quest == null) return;

            playerXP.AddAndSave(quest.RewardXP);
            playerGold.AddAndSave(quest.RewardGold);

            if (additionalGatesRevealed != null)
                additionalGatesRevealed.Set(true);

            OnRewardsGiven.Invoke();
        }
    }
}
