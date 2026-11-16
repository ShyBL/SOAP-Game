namespace SOAP
{
    /// <summary>
    /// Generic, structural quest lifecycle events only — these never grow per
    /// piece of content, they're raised regardless of *which* quest (the
    /// payload carries that). Content-specific events (a survivor rescued,
    /// an enemy killed, an item picked up) are NOT enum entries — they're
    /// free-form strings, authored as data per trigger-script instance and
    /// matched against QuestObjectiveSO.EventId. See GameChannel's string-keyed
    /// Raise/Subscribe overloads and EventIdFieldDrawer.
    ///
    /// A new quest's new content never requires editing this enum. If you're
    /// tempted to add a content-specific entry here, it belongs as an EventId
    /// string instead.
    /// </summary>
    public enum GameTopic
    {
        None,
        Events,
        PlayerStamina,
        EnemyHealth,

        QuestObjectiveUpdated,
        QuestCompleted,
        QuestOffered,
        QuestAccepted,
        QuestDeclined,
        QuestTurnedIn
    }
}