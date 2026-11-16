namespace SOAP
{
    /// <summary>
    /// Pure query layer between NPC data and quest state.
    /// No Unity dependencies. No switch statements. No type knowledge.
    /// No ScriptableObjects created — reads existing SOAP variable state only.
    ///
    /// Two queries, called by the zone components at interaction time:
    ///
    ///   GetNextOfferable  — finds the first quest not yet active, not complete,
    ///                       and with its Prerequisites satisfied.
    ///                       Used by QuestOfferZone to decide what to offer.
    ///
    ///   GetPendingTurnIn  — finds the first quest that is active AND complete
    ///                       but not yet turned in (TurnedIn flag is false).
    ///                       Used by QuestTurnInZone to decide what to accept.
    ///
    /// Both return null if no matching quest exists, which the calling zone
    /// uses as a guard — no quest to offer, or nothing ready to turn in.
    ///
    /// Sequential progression is enforced by GetNextOfferable: quest [N+1]
    /// will not be returned while quest [N] is active but incomplete, because
    /// only quests where QuestActive == false qualify as offerable. A quest
    /// whose Prerequisites aren't met blocks everything after it too, on the
    /// assumption prerequisite chains are authored in order.
    /// </summary>
    public static class QuestGiverFactory
    {
        /// <summary>
        /// Returns the first quest in the NPC's list that has not yet been
        /// activated and whose prerequisites are satisfied. Returns null if
        /// all quests are active/complete, the next one is prerequisite-gated,
        /// or the npc argument is null.
        ///
        /// Called by QuestOfferZone.Interact before raising QuestOffered.
        /// </summary>
        public static QuestDefinitionSO GetNextOfferable(NpcQuestGiver npc)
        {
            if (npc?.Quests == null) return null;

            foreach (var quest in npc.Quests)
            {
                if (quest == null) continue;
                if (!quest.PrerequisitesMet()) return null;   // blocked — nothing later can be offerable either

                // A quest is offerable when it has never been activated.
                // QuestActive.currentValue being false means the player
                // has not yet accepted it from this NPC.
                var active = quest.QuestActive;

                if (active != null && !active.currentValue)
                    return quest;
            }

            return null;    // all quests offered or no quests defined
        }

        /// <summary>
        /// Returns the first quest in the NPC's list that the player has
        /// completed but not yet turned in. Returns null if nothing is
        /// ready for turn-in, or if the npc argument is null.
        ///
        /// Called by QuestTurnInZone.Interact before accepting a turn-in.
        /// A quest is ready for turn-in when:
        ///   QuestActive   == true   (player accepted it)
        ///   QuestComplete == true   (all objectives satisfied)
        ///   TurnedIn      == false  (not already handed in)
        /// </summary>
        public static QuestDefinitionSO GetPendingTurnIn(NpcQuestGiver npc)
        {
            if (npc?.Quests == null) return null;

            foreach (var quest in npc.Quests)
            {
                if (quest == null) continue;

                var active   = quest.QuestActive;
                var complete = quest.QuestComplete;
                var turnedIn = quest.QuestTurnedIn;

                bool isActive    = active   != null && active.currentValue;
                bool isComplete  = complete != null && complete.currentValue;
                bool notTurnedIn = turnedIn != null && !turnedIn.currentValue;

                if (isActive && isComplete && notTurnedIn)
                    return quest;
            }

            return null;    // nothing ready for turn-in
        }
    }
}