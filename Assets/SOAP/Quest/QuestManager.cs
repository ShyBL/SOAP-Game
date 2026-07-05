using System;
using System.Collections.Generic;
using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Generic quest runner. No quest-specific knowledge — no hardcoded topics,
    /// thresholds, field names, or completion logic.
    ///
    /// On Enable it reads ActiveQuests, builds a topic → binding map directly
    /// from each quest's QuestObjectiveSO assets, and subscribes once per unique
    /// GameTopic. On Disable it tears everything down cleanly.
    ///
    /// Inspector setup:
    ///   questChannel → QuestChannel SO (shared across all quests)
    ///   activeQuests → any number of QuestDefinitionSO assets
    ///
    /// To reload quests (e.g. after a scene transition), disable and re-enable
    /// this component to rebuild the runtime state.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        [SerializeField] private GameChannel questChannel;
        [SerializeField] private QuestDefinition[] activeQuests;

        // ── Runtime data ──────────────────────────────────────────────────────
        // Built on Enable, cleared on Disable. Never serialized.

        /// <summary>
        /// Pairs a QuestObjectiveSO with its owning quest so Dispatch can call
        /// OnEventFired on the right objective and check the right quest for
        /// completion — all through the SO itself, no separate handler object.
        /// </summary>
        private struct ObjectiveBinding
        {
            public QuestObjective Objective;
            public QuestDefinition Quest;
        }

        /// <summary>
        /// Maps each GameTopic to every binding that cares about it across all
        /// active quests. One topic can serve bindings in multiple quests.
        /// </summary>
        private Dictionary<GameTopic, List<ObjectiveBinding>> bindingMap;

        /// <summary>
        /// Stores the exact Action delegate instances subscribed to GameChannel
        /// so they can be precisely matched on Unsubscribe. Lambdas with closures
        /// are reference-equal only through the stored instance.
        /// </summary>
        private Dictionary<GameTopic, Action<object>> subscriptionCallbacks;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnValidate()
        {
            if (questChannel == null)
                Debug.LogError($"[QuestManager] QuestChannel is not assigned on {name}!", this);
        }

        private void OnEnable()  => BuildRuntime();
        private void OnDisable() => TeardownRuntime();

        // ── Build / Teardown ──────────────────────────────────────────────────

        private void BuildRuntime()
        {
            bindingMap            = new Dictionary<GameTopic, List<ObjectiveBinding>>();
            subscriptionCallbacks = new Dictionary<GameTopic, Action<object>>();

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

                    var topic   = objective.TopicTrigger;
                    var binding = new ObjectiveBinding { Objective = objective, Quest = quest };

                    if (!bindingMap.ContainsKey(topic))
                        bindingMap[topic] = new List<ObjectiveBinding>();

                    bindingMap[topic].Add(binding);
                }
            }

            // Subscribe once per unique topic — not once per objective.
            // Capture by value so each lambda closes over its own topic copy.
            foreach (var topic in bindingMap.Keys)
            {
                var capturedTopic                    = topic;
                Action<object> callback              = data => Dispatch(capturedTopic, data);
                subscriptionCallbacks[capturedTopic] = callback;
                questChannel.Subscribe(capturedTopic, callback);
            }
        }

        private void TeardownRuntime()
        {
            if (subscriptionCallbacks != null)
            {
                foreach (var kvp in subscriptionCallbacks)
                    questChannel.Unsubscribe(kvp.Key, kvp.Value);
            }

            bindingMap            = null;
            subscriptionCallbacks = null;
        }

        // ── Dispatch ──────────────────────────────────────────────────────────

        /// <summary>
        /// Called by GameChannel when a subscribed topic fires. Routes to every
        /// binding for that topic, skipping quests that are inactive or complete.
        /// </summary>
        private void Dispatch(GameTopic topic, object data)
        {
            if (!bindingMap.TryGetValue(topic, out var bindings)) return;

            foreach (var binding in bindings)
            {
                var quest = binding.Quest;

                if (quest.QuestActive != null && !quest.QuestActive.currentValue) continue;
                if (quest.QuestComplete != null && quest.QuestComplete.currentValue) continue;

                binding.Objective.OnEventFired();
                questChannel.Raise(GameTopic.QuestObjectiveUpdated);
                CheckCompletion(quest);
            }
        }

        // ── Completion ────────────────────────────────────────────────────────

        /// <summary>
        /// Iterates the quest's own Objectives array — each SO knows its own
        /// IsComplete state. No secondary data structure needed.
        /// </summary>
        private void CheckCompletion(QuestDefinition quest)
        {
            foreach (var objective in quest.Objectives)
                if (!objective.IsComplete) return;

            quest.QuestComplete.Set(true);
            questChannel.Raise(GameTopic.QuestCompleted);
        }
    }
}