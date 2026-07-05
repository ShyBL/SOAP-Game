namespace SOAP
{
    public enum GameTopic
    {
        None,
        Events,
        PlayerStamina,
        EnemyHealth,
        
        // Quest System
        QuestObjectiveUpdated,
        QuestCompleted,
        QuestItemPickedUp,
        SurvivorRescued,
        EnemyKilled,
        EchoDiscovered,
        BossDefeated,
        PortalSealed,
        QuestOffered,
        QuestTurnedIn
    }
}