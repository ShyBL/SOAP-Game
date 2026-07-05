namespace SOAP
{
    /// <summary>
    /// Pure query layer between NPC data and quest state.
    /// No Unity dependencies. No switch statements. No type knowledge.
    /// No ScriptableObjects created — reads existing SOAP variable state only.
    ///
    /// Two queries, called by the zone components at interaction time:
    ///
    ///   GetNextOfferable  — finds the first quest not yet active and not complete.
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
    /// only quests where QuestActive == false qualify as offerable.
    /// </summary>
    public static class QuestGiverFactory
    {
        /// <summary>
        /// Returns the first quest in the NPC's list that has not yet been
        /// activated. Returns null if all quests are active or complete,
        /// or if the npc argument is null.
        ///
        /// Called by QuestOfferZone.ApplyEffect before activating a quest.
        /// </summary>
        public static QuestDefinition GetNextOfferable(NpcQuestGiver npc)
        {
            if (npc?.Quests == null) return null;

            foreach (var quest in npc.Quests)
            {
                if (quest == null) continue;

                // A quest is offerable when it has never been activated.
                // QuestActive.currentValue being false means the player
                // has not yet accepted it from this NPC.
                var active   = quest.QuestActive;
                var complete = quest.QuestComplete;

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
        /// Called by QuestTurnInZone.ApplyEffect before accepting a turn-in.
        /// A quest is ready for turn-in when:
        ///   QuestActive   == true   (player accepted it)
        ///   QuestComplete == true   (all objectives satisfied)
        ///   TurnedIn      == false  (not already handed in)
        /// </summary>
        public static QuestDefinition GetPendingTurnIn(NpcQuestGiver npc)
        {
            if (npc?.Quests == null) return null;

            foreach (var quest in npc.Quests)
            {
                if (quest == null) continue;

                var active   = quest.QuestActive;
                var complete = quest.QuestComplete;
                var turnedIn = quest.TurnedIn;

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