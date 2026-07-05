using UnityEngine;
using UnityEngine.Events;

namespace SOAP
{
    /// <summary>
    /// Grants XP, Gold, and story flags when MQ_001 completes.
    /// Fully inspector-driven — this script contains NO channel subscription code.
    ///
    /// Inspector setup:
    ///   playerXP               → PlayerXP_SavedInt SO     (saveKey: "player_xp")
    ///   playerGold             → PlayerGold_SavedInt SO   (saveKey: "player_gold")
    ///   additionalGatesRevealed→ AdditionalGatesRevealed_Bool SO
    ///   onRewardsGiven         → wire gear drop logic, return-to-captain cinematic, etc.
    ///
    /// Channel wiring (no code required):
    ///   Add a TopicResponder component to this same GameObject.
    ///   Set channel = QuestChannel, topicToListenFor = QuestCompleted.
    ///   Wire onResponseTriggered → RewardDispenser.GiveRewards().
    ///
    /// Rewards granted by GiveRewards():
    ///   +2500 XP  (persisted)
    ///   +500  Gold (persisted)
    ///   AdditionalGatesRevealed = true  (unlocks 4 further gates in the world)
    ///   onRewardsGiven fires → handle gear drop and cinematics in the inspector
    /// </summary>
    public class RewardDispenser : MonoBehaviour
    {
        [SerializeField] private SavedIntVariable playerXP;
        [SerializeField] private SavedIntVariable playerGold;
        [SerializeField] private BoolVariable additionalGatesRevealed;
 
        [Space]
        public UnityEvent OnRewardsGiven;
 
        private void OnValidate()
        {
            if (playerXP == null)
                Debug.LogError($"[RewardDispenser] PlayerXP SavedIntVariable is not assigned on {name}!", this);
            if (playerGold == null)
                Debug.LogError($"[RewardDispenser] PlayerGold SavedIntVariable is not assigned on {name}!", this);
            if (additionalGatesRevealed == null)
                Debug.LogError($"[RewardDispenser] AdditionalGatesRevealed BoolVariable is not assigned on {name}!", this);
        }
 
        /// <summary>
        /// Called by the TopicResponder's onResponseTriggered when QuestCompleted fires.
        /// </summary>
        public void GiveRewards()
        {
            playerXP.AddAndSave(2500);
            playerGold.AddAndSave(500);
            additionalGatesRevealed.Set(true);
            OnRewardsGiven.Invoke();
        }
    }
}