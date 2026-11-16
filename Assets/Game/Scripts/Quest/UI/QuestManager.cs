using System;
using System.Collections.Generic;
using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Generic quest runner. No quest-specific knowledge — no hardcoded topics,
    /// thresholds, field names, or completion logic.
    ///
    /// Persistent singleton (DontDestroyOnLoad), matching GameManager/
    /// AudioManager/SaveManager — quests like MQ_001 span multiple scenes, so
    /// this needs to survive scene transitions rather than rebuild per scene.
    ///
    /// Objective advancement is dispatched via GameChannel's string-keyed
    /// Raise/Subscribe (content-specific EventId strings) — NOT GameTopic.
    /// Generic quest lifecycle events (QuestAccepted, QuestCompleted, etc.)
    /// still use GameTopic, since those are structural and never grow per
    /// piece of content. The two mechanisms run side by side deliberately.
    ///
    /// Objectives are completed in array order (see QuestDefinitionSO.
    /// CurrentObjective) — Dispatch only routes an event to a quest's current
    /// step, so an event for a later step is silently ignored until earlier
    /// steps clear.
    ///
    /// Persists progress via SaveSystem: periodic autosave plus force-save on
    /// Quest Accepted and Quest Completed.
    ///
    /// Inspector setup:
    ///   questChannel     → QuestChannel SO (shared across all quests)
    ///   activeQuests     → any number of QuestDefinitionSO assets
    ///   autosaveInterval → seconds between periodic saves (default 60)
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager instance;

        [SerializeField] private GameChannel questChannel;
        [SerializeField] private QuestDefinitionSO[] activeQuests;

        [Header("Persistence")]
        [Tooltip("Seconds between periodic autosaves. Also force-saves on " +
                 "Quest Accepted and Quest Completed regardless of this timer.")]
        [SerializeField] private float autosaveInterval = 60f;

        // ── Runtime data ──────────────────────────────────────────────────────

        private struct ObjectiveBinding
        {
            public QuestObjectiveSO  Objective;
            public QuestDefinitionSO Quest;
        }

        // String-keyed — EventId, not GameTopic. Content-specific advancement
        // routes entirely through this mechanism now.
        private Dictionary<string, List<ObjectiveBinding>> bindingMap;

        private Action<string, object> channelEventCallback;
        private List<string>           subscribedEventIds;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void OnValidate()
        {
            if (questChannel == null)
                Debug.LogError($"[QuestManager] QuestChannel is not assigned on {name}!", this);
        }

        private void OnEnable()
        {
            BuildRuntime();
            questChannel.Subscribe(GameTopic.QuestAccepted, OnQuestAccepted);
            LoadProgress();
            InvokeRepeating(nameof(SaveProgress), autosaveInterval, autosaveInterval);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(SaveProgress));
            questChannel.Unsubscribe(GameTopic.QuestAccepted, OnQuestAccepted);
            TeardownRuntime();
        }

        // ── Build / Teardown ──────────────────────────────────────────────────

        private void BuildRuntime()
        {
            bindingMap = new Dictionary<string, List<ObjectiveBinding>>();

            if (activeQuests == null) return;

            foreach (var quest in activeQuests)
            {
                if (quest == null)
                {
                    Debug.LogWarning("[QuestManager] Null entry in ActiveQuests — skipping.", this);
                    continue;
                }

                if (quest.Objectives == null) continue;

                foreach (var objective in quest.Objectives)
                {
                    if (objective == null)
                    {
                        Debug.LogWarning($"[QuestManager] Null objective in '{quest.QuestId}' — skipping.", this);
                        continue;
                    }

                    if (string.IsNullOrEmpty(objective.EventId))
                    {
                        Debug.LogWarning($"[QuestManager] Objective with empty EventId in '{quest.QuestId}' — skipping.", this);
                        continue;
                    }

                    var eventId = objective.EventId;
                    var binding = new ObjectiveBinding { Objective = objective, Quest = quest };

                    if (!bindingMap.ContainsKey(eventId))
                        bindingMap[eventId] = new List<ObjectiveBinding>();

                    bindingMap[eventId].Add(binding);
                }
            }

            channelEventCallback = OnChannelEvent;
            subscribedEventIds   = new List<string>(bindingMap.Keys);

            foreach (var eventId in subscribedEventIds)
                questChannel.Subscribe(eventId, channelEventCallback);
        }

        private void TeardownRuntime()
        {
            if (channelEventCallback != null && subscribedEventIds != null)
                foreach (var eventId in subscribedEventIds)
                    questChannel.Unsubscribe(eventId, channelEventCallback);

            channelEventCallback = null;
            subscribedEventIds   = null;
            bindingMap           = null;
        }

        // ── Channel callbacks ─────────────────────────────────────────────────

        private void OnChannelEvent(string eventId, object data) => Dispatch(eventId, data);

        private void OnQuestAccepted(object data)
        {
            if (data is QuestDefinitionSO quest)
                SaveQuest(quest);
        }

        // ── Dispatch ──────────────────────────────────────────────────────────

        private void Dispatch(string eventId, object data)
        {
            if (!bindingMap.TryGetValue(eventId, out var bindings)) return;

            foreach (var binding in bindings)
            {
                var quest = binding.Quest;

                if (quest.QuestActive   != null && !quest.QuestActive.currentValue)   continue;
                if (quest.QuestComplete != null &&  quest.QuestComplete.currentValue)  continue;
                if (quest.CurrentObjective != binding.Objective) continue; // only the current ordered step advances

                binding.Objective.OnEventFired();
                questChannel.Raise(GameTopic.QuestObjectiveUpdated);
                CheckCompletion(quest);
            }
        }

        // ── Completion ────────────────────────────────────────────────────────

        private void CheckCompletion(QuestDefinitionSO quest)
        {
            foreach (var objective in quest.Objectives)
                if (!objective.IsComplete) return;

            quest.QuestComplete.Set(true);
            questChannel.Raise(GameTopic.QuestCompleted, quest);
            SaveQuest(quest);
        }

        // ── Persistence ───────────────────────────────────────────────────────

        public void SaveQuest(QuestDefinitionSO quest)
        {
            StageQuestSave(quest);
            SaveSystem.Flush();
        }

        public void SaveProgress()
        {
            if (activeQuests == null) return;

            foreach (var quest in activeQuests)
            {
                if (quest == null) continue;
                StageQuestSave(quest);
            }

            SaveSystem.Flush();
        }

        private void StageQuestSave(QuestDefinitionSO quest)
        {
            if (quest == null) return;

            bool isActive   = quest.QuestActive   != null && quest.QuestActive.currentValue;
            bool isComplete = quest.QuestComplete != null && quest.QuestComplete.currentValue;
            if (!isActive && !isComplete) return;

            if (quest.QuestActive != null)
                SaveSystem.Set(ActiveKey(quest), quest.QuestActive.currentValue.ToString());
            if (quest.QuestComplete != null)
                SaveSystem.Set(CompleteKey(quest), quest.QuestComplete.currentValue.ToString());
            if (quest.QuestTurnedIn != null)
                SaveSystem.Set(TurnedInKey(quest), quest.QuestTurnedIn.currentValue.ToString());

            if (quest.Objectives == null) return;

            foreach (var objective in quest.Objectives)
            {
                if (objective == null) continue;
                SaveSystem.Set(ObjectiveKey(quest, objective), objective.SaveValue);
            }
        }

        private void LoadProgress()
        {
            if (activeQuests == null) return;

            foreach (var quest in activeQuests)
            {
                if (quest == null) continue;

                if (quest.QuestActive != null)
                {
                    var loaded = SaveSystem.RequestLoad(ActiveKey(quest));
                    if (!string.IsNullOrEmpty(loaded) && bool.TryParse(loaded, out var value))
                        quest.QuestActive.Set(value);
                }

                if (quest.QuestComplete != null)
                {
                    var loaded = SaveSystem.RequestLoad(CompleteKey(quest));
                    if (!string.IsNullOrEmpty(loaded) && bool.TryParse(loaded, out var value))
                        quest.QuestComplete.Set(value);
                }

                if (quest.QuestTurnedIn != null)
                {
                    var loaded = SaveSystem.RequestLoad(TurnedInKey(quest));
                    if (!string.IsNullOrEmpty(loaded) && bool.TryParse(loaded, out var value))
                        quest.QuestTurnedIn.Set(value);
                }

                if (quest.Objectives == null) continue;

                foreach (var objective in quest.Objectives)
                {
                    if (objective == null) continue;
                    var loaded = SaveSystem.RequestLoad(ObjectiveKey(quest, objective));
                    if (!string.IsNullOrEmpty(loaded))
                        objective.LoadValue(loaded);
                }
            }
        }

        private static string ActiveKey(QuestDefinitionSO quest)     => $"{quest.QuestId}_active";
        private static string CompleteKey(QuestDefinitionSO quest)   => $"{quest.QuestId}_complete";
        private static string TurnedInKey(QuestDefinitionSO quest)   => $"{quest.QuestId}_turnedin";
        private static string ObjectiveKey(QuestDefinitionSO quest, QuestObjectiveSO objective)
            => $"{quest.QuestId}_{objective.ObjectiveId}";
    }
}