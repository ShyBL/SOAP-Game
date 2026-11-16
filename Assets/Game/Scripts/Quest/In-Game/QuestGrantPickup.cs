using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Grants a quest with no NPC giver (e.g. the Prologue). Mirrors exactly
    /// what QuestOfferUIController.Accept() does (QuestActive.Set + QuestAccepted
    /// raise) so QuestManager's force-save and QuestJournalUI pick it up
    /// identically to an NPC-offered quest.
    ///
    /// Two ways to use it:
    ///   1. Trigger mode (default): the player touches this trigger and the
    ///      quest is granted.
    ///   2. Item mode: untick grantOnTrigger, put an ItemPickup on the same
    ///      object, and wire ItemPickup.onCollected → QuestGrantPickup.Grant.
    ///      The scroll is then stored in the backpack AND grants the quest.
    ///
    /// Inspector setup:
    ///   questChannel   → QuestChannel SO
    ///   grantedQuest   → the QuestDefinitionSO to activate
    ///   grantOnTrigger → true for trigger mode, false for item mode
    /// </summary>
    public class QuestGrantPickup : CollisionHandler
    {
        [SerializeField] private GameChannel questChannel;
        [SerializeField] private QuestDefinitionSO grantedQuest;

        [Tooltip("False when something else (e.g. ItemPickup.onCollected) calls Grant() instead " +
                 "of the player walking into this trigger.")]
        [SerializeField] private bool grantOnTrigger = true;

        private void OnValidate()
        {
            if (questChannel == null)
                Debug.LogError($"[QuestGrantPickup] QuestChannel is not assigned on {name}!", this);
            if (grantedQuest == null)
                Debug.LogError($"[QuestGrantPickup] GrantedQuest is not assigned on {name}!", this);
        }

        private void Start()
        {
            // QuestActive is restored from the save, but this scene object is not:
            // hide the pickup when its quest was already granted in a previous session.
            if (grantedQuest != null && grantedQuest.QuestActive != null && grantedQuest.QuestActive.currentValue)
                gameObject.SetActive(false);
        }

        protected override void ApplyEffect(GameObject other)
        {
            if (!grantOnTrigger) return;
            if (!other.CompareTag("Player")) return;

            Grant();
        }

        /// <summary>Activates the quest. Safe to call repeatedly — it only fires once.</summary>
        public void Grant()
        {
            if (grantedQuest.QuestActive == null || grantedQuest.QuestActive.currentValue) return;

            grantedQuest.QuestActive.Set(true);
            questChannel.Raise(GameTopic.QuestAccepted, grantedQuest);
            gameObject.SetActive(false);
        }
    }
}