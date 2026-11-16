using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// One asset per quest-giving NPC. Owns the ordered list of quests
    /// this NPC can offer and accept as turn-ins.
    ///
    /// Designer setup — do this once per NPC, never again:
    ///   NpcId       → stable identifier matching your dialogue/NPC system
    ///   DisplayName → "Captain Aldric Ironwall"
    ///   Quests      → drag in QuestDefinitionSO assets in offer order
    ///
    /// Quest progression is sequential: the NPC offers quest [0], then [1],
    /// then [2], etc. A quest must be complete before the next is offered.
    /// QuestGiverFactory reads QuestActive, QuestComplete, and Prerequisites
    /// on each QuestDefinitionSO to determine which quest is currently
    /// actionable.
    ///
    /// To add a new quest to this NPC later: open this asset, append to Quests.
    /// Nothing in the scene changes.
    /// </summary>
    [CreateAssetMenu(fileName = "New_NpcQuestGiver", menuName = "SOAP/Quest/NPC Quest Giver")]
    public class NpcQuestGiver : Npc
    {
        [Header("Quests")]
        [Tooltip("All quests this NPC can give, in offer order. " +
                 "Quest [0] is offered first. Each subsequent quest unlocks " +
                 "only after the previous one is complete.")]
        public QuestDefinitionSO[] Quests;

        /// <summary>
        /// True when this NPC currently has a quest ready to offer. Drives the
        /// "!" indicator (NpcMapMarker) — same query the offer flow itself uses,
        /// so the indicator can never show stale state.
        /// </summary>
        public bool HasOfferableQuest => QuestGiverFactory.GetNextOfferable(this) != null;

        /// <summary>
        /// True when this NPC has a completed quest waiting on turn-in. Drives
        /// the "?" indicator (NpcMapMarker).
        /// </summary>
        public bool HasPendingTurnIn => QuestGiverFactory.GetPendingTurnIn(this) != null;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(NpcId))
                Debug.LogWarning($"[NpcQuestGiverSO] '{name}': NpcId is empty. " +
                                 "Set it to match your NPC/dialogue system.", this);

            if (Quests == null || Quests.Length == 0)
                Debug.LogWarning($"[NpcQuestGiverSO] '{name}': Quests array is empty.", this);
        }
    }
}