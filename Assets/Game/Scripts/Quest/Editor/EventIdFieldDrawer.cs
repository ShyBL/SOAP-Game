using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SOAP
{
    [CustomPropertyDrawer(typeof(EventIdFieldAttribute))]
    public class EventIdFieldDrawer : PropertyDrawer
    {
        private const float ButtonWidth = 20f;

        private static List<string> cachedIds = new List<string>();
        private static double cacheTime = -1;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.HelpBox(position,
                    $"[EventIdField] on '{property.name}' requires a string field.",
                    MessageType.Warning);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            float fieldWidth = position.width - ButtonWidth - 2f;
            var fieldRect  = new Rect(position.x, position.y, fieldWidth, position.height);
            var buttonRect = new Rect(position.x + fieldWidth + 2f, position.y, ButtonWidth, position.height);

            EditorGUI.PropertyField(fieldRect, property, label);

            if (GUI.Button(buttonRect, "▾"))
                ShowKnownIdsMenu(property);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight;

        private static void ShowKnownIdsMenu(SerializedProperty property)
        {
            var ids = GetKnownIds();
            var menu = new GenericMenu();

            if (ids.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No EventIds found yet — type a new one directly"));
            }
            else
            {
                foreach (var id in ids)
                {
                    var captured = id;
                    menu.AddItem(new GUIContent(captured), false, () =>
                    {
                        property.stringValue = captured;
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }
            }

            menu.ShowAsContext();
        }

        /// <summary>
        /// Scans every QuestObjectiveSO asset in the project for its EventId
        /// value. Covers project assets only, not trigger-script fields living
        /// in scenes — the asset-side list is the authoritative one that
        /// actually matters for reuse.
        /// </summary>
        private static List<string> GetKnownIds()
        {
            if (cacheTime >= 0 && EditorApplication.timeSinceStartup - cacheTime < 1.0)
                return cachedIds;

            var ids = new HashSet<string>();
            var guids = AssetDatabase.FindAssets("t:QuestObjectiveSO");

            foreach (var guid in guids)
            {
                var path      = AssetDatabase.GUIDToAssetPath(guid);
                var objective = AssetDatabase.LoadAssetAtPath<QuestObjectiveSO>(path);

                if (objective != null && !string.IsNullOrEmpty(objective.EventId))
                    ids.Add(objective.EventId);
            }

            cachedIds = ids.OrderBy(id => id).ToList();
            cacheTime = EditorApplication.timeSinceStartup;

            return cachedIds;
        }
    }
}
