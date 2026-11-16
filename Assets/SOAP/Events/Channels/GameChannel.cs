using System;
using System.Collections.Generic;
using UnityEngine;

namespace SOAP
{
    [CreateAssetMenu(fileName = "New_GameChannel", menuName = "SOAP/Events/Channel")]
    public class GameChannel : ScriptableObject
    {
        [Header("Debug")]
        public bool debugLogEnabled = false;

        // ── GameTopic-keyed listener dictionaries ───────────────────────────────
        // For structural, generic lifecycle events (QuestAccepted, QuestCompleted,
        // etc.) that never grow per piece of content — see string-keyed section
        // below for content-specific events (which quest, which trigger fired).

        // Original overload — Action<object> — used by TopicResponder and all
        // single-topic subscribers. Untouched for backward compatibility.
        private readonly Dictionary<GameTopic, Action<object>> _listeners
            = new Dictionary<GameTopic, Action<object>>();

        // Action<GameTopic, object> — for dynamic multi-topic subscribers
        // (e.g. QuestManager's generic lifecycle subscriptions) that need to
        // know which topic fired without closures.
        private readonly Dictionary<GameTopic, Action<GameTopic, object>> _topicListeners
            = new Dictionary<GameTopic, Action<GameTopic, object>>();

        // ── String-keyed listener dictionaries ───────────────────────────────────
        // For content-specific events — arbitrary IDs authored as data per
        // trigger-script instance and per QuestObjectiveSO, not hardcoded as
        // enum literals in shared framework code. Same two-shape split as the
        // GameTopic-keyed dictionaries above, for the same reason.

        private readonly Dictionary<string, Action<object>> _stringListeners
            = new Dictionary<string, Action<object>>();

        private readonly Dictionary<string, Action<string, object>> _stringTopicListeners
            = new Dictionary<string, Action<string, object>>();

        // ── Raise ─────────────────────────────────────────────────────────────

        public void Raise(GameTopic topic, object data = null)
        {
            if (_listeners.TryGetValue(topic, out var action))
                action.Invoke(data);

            if (_topicListeners.TryGetValue(topic, out var topicAction))
                topicAction.Invoke(topic, data);

            if (debugLogEnabled)
                Debug.Log($"[GameChannel] {name} raised topic '{topic}'. Data: {data}");
        }

        public void Raise(string eventId, object data = null)
        {
            if (_stringListeners.TryGetValue(eventId, out var action))
                action.Invoke(data);

            if (_stringTopicListeners.TryGetValue(eventId, out var topicAction))
                topicAction.Invoke(eventId, data);

            if (debugLogEnabled)
                Debug.Log($"[GameChannel] {name} raised event '{eventId}'. Data: {data}");
        }

        // ── Subscribe / Unsubscribe — GameTopic, Action<object> ────────────────

        public void Subscribe(GameTopic topic, Action<object> listener)
        {
            if (!_listeners.ContainsKey(topic))
                _listeners[topic] = delegate { };
            _listeners[topic] += listener;

#if UNITY_EDITOR
            TrackSubscribe(topic.ToString(), listener, isEvent: false);
#endif
        }

        public void Unsubscribe(GameTopic topic, Action<object> listener)
        {
            if (!_listeners.ContainsKey(topic)) return;
            _listeners[topic] -= listener;

            if (_listeners[topic] == null || _listeners[topic].GetInvocationList().Length == 0)
                _listeners.Remove(topic);

#if UNITY_EDITOR
            TrackUnsubscribe(topic.ToString(), listener, isEvent: false);
#endif
        }

        // ── Subscribe / Unsubscribe — GameTopic, Action<GameTopic, object> ─────

        public void Subscribe(GameTopic topic, Action<GameTopic, object> listener)
        {
            if (!_topicListeners.ContainsKey(topic))
                _topicListeners[topic] = delegate { };
            _topicListeners[topic] += listener;

#if UNITY_EDITOR
            TrackSubscribe(topic.ToString(), listener, isEvent: false);
#endif
        }

        public void Unsubscribe(GameTopic topic, Action<GameTopic, object> listener)
        {
            if (!_topicListeners.ContainsKey(topic)) return;
            _topicListeners[topic] -= listener;

            if (_topicListeners[topic] == null || _topicListeners[topic].GetInvocationList().Length == 0)
                _topicListeners.Remove(topic);

#if UNITY_EDITOR
            TrackUnsubscribe(topic.ToString(), listener, isEvent: false);
#endif
        }

        // ── Subscribe / Unsubscribe — string, Action<object> ───────────────────

        public void Subscribe(string eventId, Action<object> listener)
        {
            if (!_stringListeners.ContainsKey(eventId))
                _stringListeners[eventId] = delegate { };
            _stringListeners[eventId] += listener;

#if UNITY_EDITOR
            TrackSubscribe(eventId, listener, isEvent: true);
#endif
        }

        public void Unsubscribe(string eventId, Action<object> listener)
        {
            if (!_stringListeners.ContainsKey(eventId)) return;
            _stringListeners[eventId] -= listener;

            if (_stringListeners[eventId] == null || _stringListeners[eventId].GetInvocationList().Length == 0)
                _stringListeners.Remove(eventId);

#if UNITY_EDITOR
            TrackUnsubscribe(eventId, listener, isEvent: true);
#endif
        }

        // ── Subscribe / Unsubscribe — string, Action<string, object> ───────────

        public void Subscribe(string eventId, Action<string, object> listener)
        {
            if (!_stringTopicListeners.ContainsKey(eventId))
                _stringTopicListeners[eventId] = delegate { };
            _stringTopicListeners[eventId] += listener;

#if UNITY_EDITOR
            TrackSubscribe(eventId, listener, isEvent: true);
#endif
        }

        public void Unsubscribe(string eventId, Action<string, object> listener)
        {
            if (!_stringTopicListeners.ContainsKey(eventId)) return;
            _stringTopicListeners[eventId] -= listener;

            if (_stringTopicListeners[eventId] == null || _stringTopicListeners[eventId].GetInvocationList().Length == 0)
                _stringTopicListeners.Remove(eventId);

#if UNITY_EDITOR
            TrackUnsubscribe(eventId, listener, isEvent: true);
#endif
        }

        // ── Editor-only subscriber tracking ───────────────────────────────────
        // Two separate dictionaries rather than one unified one — GameTopic and
        // string events are deliberately different mechanisms (structural vs
        // content), and GameChannelDrawer shows them as two distinct sections
        // rather than blending them into one list.

#if UNITY_EDITOR
        private readonly Dictionary<string, List<string>> _debugSubscribers
            = new Dictionary<string, List<string>>();

        private readonly Dictionary<string, List<string>> _debugStringSubscribers
            = new Dictionary<string, List<string>>();

        public IReadOnlyDictionary<string, List<string>> DebugSubscribers => _debugSubscribers;
        public IReadOnlyDictionary<string, List<string>> DebugStringSubscribers => _debugStringSubscribers;

        private void TrackSubscribe(string key, Delegate listener, bool isEvent)
        {
            var dict = isEvent ? _debugStringSubscribers : _debugSubscribers;

            if (!dict.ContainsKey(key))
                dict[key] = new List<string>();

            var label = BuildLabel(listener);
            if (!dict[key].Contains(label))
                dict[key].Add(label);
        }

        private void TrackUnsubscribe(string key, Delegate listener, bool isEvent)
        {
            var dict = isEvent ? _debugStringSubscribers : _debugSubscribers;

            if (!dict.TryGetValue(key, out var list)) return;
            list.Remove(BuildLabel(listener));
            if (list.Count == 0)
                dict.Remove(key);
        }

        private static string BuildLabel(Delegate listener)
        {
            var target     = listener.Target;
            var methodName = listener.Method.Name;

            // Compiler-generated names (lambdas, anonymous methods) contain '<'.
            // No SOAP subscriber uses closures, but handle gracefully in case
            // third-party code does.
            bool isGenerated = methodName.Contains('<');

            if (target is Component component)
            {
                var goName   = component.gameObject.name;
                var typeName = target.GetType().Name;
                return isGenerated
                    ? $"{goName} ({typeName})"
                    : $"{goName} ({typeName}) → {methodName}";
            }

            if (target == null)
                return isGenerated ? "[static]" : $"[static] → {methodName}";

            var className = target.GetType().Name;
            return isGenerated ? className : $"{className} → {methodName}";
        }
#endif
    }
}
