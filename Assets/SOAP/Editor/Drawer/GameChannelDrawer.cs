using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SOAP
{
    [CustomPropertyDrawer(typeof(GameChannel))]
    public class GameChannelDrawer : PropertyDrawer
    {
        private const float IndentWidth = 12f;

        private static readonly Dictionary<string, bool> foldoutStates
            = new Dictionary<string, bool>();

        private static readonly Dictionary<int, (int count, double time)> sceneRefCache
            = new Dictionary<int, (int count, double time)>();

        // ── Height ────────────────────────────────────────────────────────────

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float h    = EditorGUIUtility.singleLineHeight;
            float line = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            var channel = property.objectReferenceValue as GameChannel;
            if (channel == null) return h;

            h += line; // scene ref count row (edit) or section headers (play)

            if (!Application.isPlaying) return h;

            h += line; // "Topics" section header
            foreach (var kvp in channel.DebugSubscribers)
            {
                h += line;
                if (GetFoldoutOpen(property, "topic_" + kvp.Key))
                    h += kvp.Value.Count * line;
            }

            h += line; // "Events" section header
            foreach (var kvp in channel.DebugStringSubscribers)
            {
                h += line;
                if (GetFoldoutOpen(property, "event_" + kvp.Key))
                    h += kvp.Value.Count * line;
            }

            return h;
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var lineRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.ObjectField(lineRect, property, label);

            var channel = property.objectReferenceValue as GameChannel;
            if (channel != null)
            {
                float y = lineRect.yMax + EditorGUIUtility.standardVerticalSpacing;

                if (!Application.isPlaying)
                    DrawEditMode(position.x, position.width, ref y, channel);
                else
                    DrawPlayMode(position.x, position.width, ref y, property, channel);
            }

            EditorGUI.EndProperty();
        }

        // ── Edit mode ─────────────────────────────────────────────────────────

        private static void DrawEditMode(float x, float width, ref float y, GameChannel channel)
        {
            int count = GetSceneReferenceCount(channel);

            string msg = count == 0
                ? "No scene components reference this channel"
                : $"{count} component{(count == 1 ? "" : "s")} in scene reference this channel";

            EditorGUI.LabelField(
                new Rect(x + IndentWidth, y, width - IndentWidth, EditorGUIUtility.singleLineHeight),
                msg, EditorStyles.miniLabel);

            y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        // ── Play mode ─────────────────────────────────────────────────────────

        private static void DrawPlayMode(
            float x, float width, ref float y,
            SerializedProperty property, GameChannel channel)
        {
            float line = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.LabelField(
                new Rect(x + IndentWidth, y, width - IndentWidth, EditorGUIUtility.singleLineHeight),
                "Topics (GameTopic)", EditorStyles.boldLabel);
            y += line;
            DrawSubscriberDict(x, width, ref y, property, channel.DebugSubscribers, "topic_");

            EditorGUI.LabelField(
                new Rect(x + IndentWidth, y, width - IndentWidth, EditorGUIUtility.singleLineHeight),
                "Events (string)", EditorStyles.boldLabel);
            y += line;
            DrawSubscriberDict(x, width, ref y, property, channel.DebugStringSubscribers, "event_");
        }

        private static void DrawSubscriberDict(
            float x, float width, ref float y,
            SerializedProperty property,
            IReadOnlyDictionary<string, List<string>> subscribers,
            string keyPrefix)
        {
            float line = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (subscribers.Count == 0)
            {
                EditorGUI.LabelField(
                    new Rect(x + IndentWidth * 2, y, width - IndentWidth * 2, EditorGUIUtility.singleLineHeight),
                    "(none active)", EditorStyles.miniLabel);
                y += line;
                return;
            }

            foreach (var kvp in subscribers)
            {
                var stateKey = keyPrefix + kvp.Key;
                bool isOpen  = GetFoldoutOpen(property, stateKey);

                var rowRect = new Rect(
                    x + IndentWidth * 2, y,
                    width - IndentWidth * 2, EditorGUIUtility.singleLineHeight);

                bool newOpen = EditorGUI.Foldout(
                    rowRect, isOpen,
                    $"{kvp.Key}  ({kvp.Value.Count})",
                    toggleOnLabelClick: true);

                if (newOpen != isOpen)
                    SetFoldoutOpen(property, stateKey, newOpen);

                y += line;

                if (!newOpen) continue;

                foreach (var name in kvp.Value)
                {
                    EditorGUI.LabelField(
                        new Rect(x + IndentWidth * 3, y, width - IndentWidth * 3, EditorGUIUtility.singleLineHeight),
                        $"• {name}", EditorStyles.miniLabel);
                    y += line;
                }
            }
        }

        // ── Scene reference count ─────────────────────────────────────────────

        private static int GetSceneReferenceCount(GameChannel channel)
        {
            int id = channel.GetInstanceID();

            if (sceneRefCache.TryGetValue(id, out var cached) &&
                EditorApplication.timeSinceStartup - cached.time < 1.0)
                return cached.count;

            int count = 0;

            foreach (var mb in Object.FindObjectsOfType<MonoBehaviour>(true))
            {
                var type = mb.GetType();
                foreach (var field in type.GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (field.FieldType != typeof(GameChannel)) continue;
                    try
                    {
                        if (field.GetValue(mb) as GameChannel == channel)
                        {
                            count++;
                            break;
                        }
                    }
                    catch { /* skip unreadable fields */ }
                }
            }

            sceneRefCache[id] = (count, EditorApplication.timeSinceStartup);
            return count;
        }

        // ── Foldout state helpers ─────────────────────────────────────────────

        private static string FoldoutKey(SerializedProperty property, string suffix)
            => $"{property.serializedObject.targetObject.GetInstanceID()}_{property.propertyPath}_{suffix}";

        private static bool GetFoldoutOpen(SerializedProperty property, string suffix)
            => foldoutStates.TryGetValue(FoldoutKey(property, suffix), out var open) && open;

        private static void SetFoldoutOpen(SerializedProperty property, string suffix, bool open)
            => foldoutStates[FoldoutKey(property, suffix)] = open;
    }
}
