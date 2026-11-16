using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Place on the NPC's turn-in trigger collider (or the same NPC GameObject).
    /// Handles the moment the player returns to hand in a completed quest.
    ///
    /// Inspector setup:
    ///   questChannel → QuestChannel SO
    ///   npcId        → e.g. "captain_aldric"  (same value as QuestOfferZone)
    ///   registry     → QuestGiverRegistry SO
    ///   doOnce       → false  (player may re-approach; TurnedIn flag gates re-trigger)
    ///
    /// Flow when the player interacts while in range:
    ///   1. QuestGiverFactory queries the NPC's quest list for the first
    ///      quest where QuestActive == true, QuestComplete == true,
    ///      and TurnedIn == false.
    ///   2. If none found — nothing ready to hand in — silently returns.
    ///   3. TurnedIn is set to true → prevents this quest being handed
    ///      in a second time regardless of how many times the player
    ///      re-interacts.
    ///   4. QuestTurnedIn is raised on the channel with the quest as data.
    ///      QuestTurnInUIController shows the completion panel and blends
    ///      the camera; RewardDispenser grants rewards.
    /// </summary>
    public class QuestTurnInZone : QuestGiverZone
    {
        public override void Interact(GameObject interactor)
        {
            if (!interactor.CompareTag("Player")) return;

            var quest = QuestGiverFactory.GetPendingTurnIn(Npc);
            if (quest == null) return;      // nothing ready to turn in

            quest.QuestTurnedIn.Set(true);
            questChannel.Raise(GameTopic.QuestTurnedIn, quest);
        }
    }
}