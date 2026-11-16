using System.Collections.Generic;
using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Generic, production-facing quest journal. Tracks multiple concurrent
    /// quests at once — unlike ObjectiveTracker (which stays as a manual,
    /// per-counter debug tool), this reads any QuestDefinitionSO's Objectives
    /// directly via ProgressLabel, so it needs zero per-quest wiring.
    ///
    /// Inspector setup:
    ///   contentContainer → parent Transform for instantiated journal rows
    ///                       (e.g. a ScrollRect's Content transform)
    ///   entryPrefab      → a QuestJournalEntry prefab
    ///
    ///   Add a QuestDefinitionSOTopicResponder to this GameObject:
    ///     channel = QuestChannel, topicToListenFor = QuestAccepted
    ///     onTypedResponse → QuestJournalUI.AddTrackedQuest
    ///
    ///   Add a SECOND QuestDefinitionSOTopicResponder to this GameObject:
    ///     channel = QuestChannel, topicToListenFor = QuestCompleted
    ///     onTypedResponse → QuestJournalUI.RemoveTrackedQuest
    ///
    ///   Add any existing concrete TopicResponder (e.g. BoolTopicResponder —
    ///   the payload is unused, only onResponseTriggered matters here):
    ///     channel = QuestChannel, topicToListenFor = QuestObjectiveUpdated
    ///     onResponseTriggered → QuestJournalUI.Refresh
    /// </summary>
    public class QuestJournalUI : MonoBehaviour
    {
        [SerializeField] private Transform contentContainer;
        [SerializeField] private QuestJournalEntry entryPrefab;

        private readonly List<QuestDefinitionSO> trackedQuests = new List<QuestDefinitionSO>();
        private readonly Dictionary<QuestDefinitionSO, QuestJournalEntry> activeEntries
            = new Dictionary<QuestDefinitionSO, QuestJournalEntry>();

        private void OnValidate()
        {
            if (contentContainer == null)
                Debug.LogError($"[QuestJournalUI] ContentContainer is not assigned on {name}!", this);
            if (entryPrefab == null)
                Debug.LogError($"[QuestJournalUI] EntryPrefab is not assigned on {name}!", this);
        }

        /// <summary>
        /// Wired to the QuestAccepted responder. Adds the quest to the
        /// tracked list and instantiates a row for it, unless it's already
        /// tracked (defensive — shouldn't normally happen since an active
        /// quest can't be re-offered).
        /// </summary>
        public void AddTrackedQuest(QuestDefinitionSO quest)
        {
            if (quest == null || trackedQuests.Contains(quest)) return;

            trackedQuests.Add(quest);

            var entry = Instantiate(entryPrefab, contentContainer);
            entry.Populate(quest);
            activeEntries[quest] = entry;
        }

        /// <summary>
        /// Wired to the QuestCompleted responder. Removes the quest's row —
        /// this is the direct payoff of QuestCompleted now carrying the quest
        /// as payload: the journal knows exactly which entry to drop instead
        /// of needing to guess or clear everything.
        /// </summary>
        public void RemoveTrackedQuest(QuestDefinitionSO quest)
        {
            if (quest == null) return;

            trackedQuests.Remove(quest);

            if (activeEntries.TryGetValue(quest, out var entry))
            {
                if (entry != null) Destroy(entry.gameObject);
                activeEntries.Remove(quest);
            }
        }

        /// <summary>
        /// Wired to the QuestObjectiveUpdated responder's onResponseTriggered.
        /// The event doesn't say which quest's objective changed, so the
        /// cheap, correct move is refreshing every tracked row rather than
        /// trying to isolate the change.
        /// </summary>
        public void Refresh()
        {
            foreach (var quest in trackedQuests)
            {
                if (activeEntries.TryGetValue(quest, out var entry))
                    entry.Populate(quest);
            }
        }
    }
}
