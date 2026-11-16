using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Place on the NPC's offer trigger collider (or the NPC GameObject itself).
    /// Handles the moment the player accepts a quest from this NPC.
    ///
    /// Inspector setup:
    ///   questChannel → QuestChannel SO
    ///   npcId        → e.g. "captain_aldric"
    ///   registry     → QuestGiverRegistry SO
    ///   doOnce       → false
    ///
    /// Flow when the player interacts while in range:
    ///   1. QuestGiverFactory queries the NPC's quest list for the first
    ///      offerable quest (QuestActive == false).
    ///   2. If none found — all quests already offered — silently returns.
    ///   3. QuestOffered is raised on the channel with the quest as payload.
    ///      QuestOfferUIController hears this, shows the offer panel, and
    ///      only sets QuestActive = true if the player presses Accept.
    ///      This zone no longer touches QuestActive directly.
    /// </summary>
    public class QuestOfferZone : QuestGiverZone
    {
        public override void Interact(GameObject interactor)
        {
            if (!interactor.CompareTag("Player")) return;

            var quest = QuestGiverFactory.GetNextOfferable(Npc);
            if (quest == null) return;      // nothing to offer right now

            questChannel.Raise(GameTopic.QuestOffered, quest);
        }
    }
}