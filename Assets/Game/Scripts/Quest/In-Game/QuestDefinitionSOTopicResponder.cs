namespace SOAP
{
    /// <summary>
    /// TopicResponder specialized for QuestDefinitionSO payloads.
    ///
    /// Shared by every component that needs to receive "which quest" from a
    /// channel event: QuestOfferUIController (QuestOffered), QuestTurnInUIController
    /// (QuestTurnedIn), RewardDispenser (QuestCompleted), and QuestJournalUI
    /// (QuestAccepted AND QuestCompleted — two responder components on the same
    /// GameObject, each with a different topicToListenFor).
    ///
    /// Same one-line-subclass pattern as BoolTopicResponder, IntTopicResponder,
    /// FloatTopicResponder, StringTopicResponder.
    /// </summary>
    public class QuestDefinitionSOTopicResponder : TopicResponder<QuestDefinitionSO>
    {

    }
}
