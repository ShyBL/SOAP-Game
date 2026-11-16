using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SOAP
{
    [CustomPropertyDrawer(typeof(SOCreateAttribute))]
    public class SOCreateDrawer : PropertyDrawer
    {
        private const float ButtonWidth = 58f;
        private const float Spacing     = 2f;

        // ── Persistent state ──────────────────────────────────────────────────
        // Static so state survives inspector repaints. Cleared on domain reload
        // to avoid holding stale SerializedObject references across recompiles.

        private static readonly Dictionary<string, bool>             foldoutStates = new Dictionary<string, bool>();
        private static readonly Dictionary<string, SerializedObject> soCache       = new Dictionary<string, SerializedObject>();

        static SOCreateDrawer()
        {
            AssemblyReloadEvents.beforeAssemblyReload += ClearCache;
        }

        private static void ClearCache()
        {
            foreach (var so in soCache.Values)
                so?.Dispose();

            soCache.Clear();
            foldoutStates.Clear();
        }

        // ── Height ────────────────────────────────────────────────────────────

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (property.propertyType != SerializedPropertyType.ObjectReference) return height;
            if (property.objectReferenceValue == null)                            return height;
            if (!GetFoldoutOpen(property))                                        return height;

            var so = GetOrCreateSO(property);
            if (so == null) return height;

            so.Update();

            var iter = so.GetIterator();
            iter.NextVisible(true); // step into children, skipping m_Script

            while (iter.NextVisible(false))
                height += EditorGUI.GetPropertyHeight(iter, true) + EditorGUIUtility.standardVerticalSpacing;

            height += EditorGUIUtility.standardVerticalSpacing; // bottom padding

            return height;
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.HelpBox(position,
                    $"[SOCreate] on '{property.name}' requires a ScriptableObject reference field.",
                    MessageType.Warning);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            var lineRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            if (property.objectReferenceValue == null)
                DrawNullState(lineRect, property, label);
            else
                DrawAssignedState(position, lineRect, property, label);

            EditorGUI.EndProperty();
        }

        // ── Null state ────────────────────────────────────────────────────────

        private void DrawNullState(Rect lineRect, SerializedProperty property, GUIContent label)
        {
            float fieldWidth = lineRect.width - ButtonWidth - Spacing;
            var   fieldRect  = new Rect(lineRect.x,                        lineRect.y, fieldWidth,  lineRect.height);
            var   buttonRect = new Rect(lineRect.x + fieldWidth + Spacing, lineRect.y, ButtonWidth, lineRect.height);

            EditorGUI.ObjectField(fieldRect, property, label);

            if (GUI.Button(buttonRect, "Create"))
                OnCreateClicked(property);
        }

        // ── Assigned state ────────────────────────────────────────────────────

        private void DrawAssignedState(Rect position, Rect lineRect, SerializedProperty property, GUIContent label)
        {
            // Split the header row: foldout toggle + label on the left,
            // object reference slot on the right.
            float labelWidth = EditorGUIUtility.labelWidth;
            var foldoutRect  = new Rect(lineRect.x,              lineRect.y, labelWidth,                lineRect.height);
            var fieldRect    = new Rect(lineRect.x + labelWidth, lineRect.y, lineRect.width - labelWidth, lineRect.height);

            bool isOpen  = GetFoldoutOpen(property);
            bool newOpen = EditorGUI.Foldout(foldoutRect, isOpen, label, toggleOnLabelClick: true);

            if (newOpen != isOpen)
                SetFoldoutOpen(property, newOpen);

            // Object slot without a label — label area is owned by the foldout.
            EditorGUI.ObjectField(fieldRect, property, GUIContent.none);

            if (!newOpen) return;

            // ── Inline property fields ─────────────────────────────────────────

            var so = GetOrCreateSO(property);
            if (so == null) return;

            so.Update();

            EditorGUI.indentLevel++;

            float y    = lineRect.yMax + EditorGUIUtility.standardVerticalSpacing;
            var   iter = so.GetIterator();
            iter.NextVisible(true); // skip m_Script

            while (iter.NextVisible(false))
            {
                float propHeight = EditorGUI.GetPropertyHeight(iter, true);
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, propHeight), iter, true);
                y += propHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            EditorGUI.indentLevel--;

            if (so.hasModifiedProperties)
                so.ApplyModifiedProperties();
        }

        // ── Create ────────────────────────────────────────────────────────────

        // private void OnCreateClicked(SerializedProperty property)
        // {
        //     var type = ResolveFieldType();
        //
        //     if (type == null || !typeof(ScriptableObject).IsAssignableFrom(type))
        //     {
        //         Debug.LogWarning(
        //             $"[SOCreate] '{property.name}': could not resolve a valid ScriptableObject type.");
        //         return;
        //     }
        //
        //     var folder = ResolveFolder(property.serializedObject.targetObject);
        //
        //     if (type.IsAbstract)
        //         ScriptableObjectSubtypeMenu.Show(type, folder, instance => AssignToProperty(property, instance));
        //     else
        //         AssignToProperty(property, ScriptableObjectSubtypeMenu.CreateAsset(type, folder));
        // }

        private void OnCreateClicked(SerializedProperty property)
        {
            var type = ResolveFieldType();

            if (type == null || !typeof(ScriptableObject).IsAssignableFrom(type))
            {
                Debug.LogWarning(
                    $"[SOCreate] '{property.name}': could not resolve a valid ScriptableObject type.");
                return;
            }

            // Captured now, not held as a live SerializedProperty — EditorInputPrompt
            // is non-modal, so by the time the user confirms, this property may be
            // stale. Owner + path + raw field name are safe to carry across that gap.
            var ownerObject  = property.serializedObject.targetObject;
            var propertyPath = property.propertyPath;
            var fieldName    = property.name; // raw field name, NOT the nicified displayName
            var folder       = ResolveFolder(ownerObject);

            if (type.IsAbstract)
                ScriptableObjectSubtypeMenu.PickType(type,
                    concreteType => PromptAndCreate(concreteType, ownerObject, propertyPath, fieldName, folder));
            else
                PromptAndCreate(type, ownerObject, propertyPath, fieldName, folder);
        }

        private static void PromptAndCreate(Type concreteType, UnityEngine.Object ownerObject,
            string propertyPath, string fieldName, string folder)
        {
            if (ownerObject == null) return; // owner deleted while the subtype menu was open

            var defaultName = BuildDefaultName(ownerObject, fieldName, concreteType);

            EditorInputPrompt.Show("Create Asset", "Asset Name", defaultName, assetName =>
            {
                if (string.IsNullOrEmpty(assetName)) return;
                if (ownerObject == null) return; // owner deleted while the prompt was open

                var instance = ScriptableObjectSubtypeMenu.CreateAsset(concreteType, folder, assetName);
                AssignToPath(ownerObject, propertyPath, instance);
            });
        }

        /// <summary>
        /// Pre-filled default asset name. Shape depends on which naming-context
        /// interfaces the owner implements:
        ///
        ///   ISOCreateVariableNamingContext present  → "{Prefix}_Objective{VarLabel}_{ShortName}"
        ///     e.g. MQ_001_ObjectiveCounter_EchoesFound  (owner: a QuestObjectiveSO)
        ///
        ///   ISOCreateNamingContext only              → "{Prefix}_{FieldName}_{TypeName}"
        ///     e.g. MQ_001_QuestActive_BoolVariable      (owner: QuestDefinitionSO)
        ///
        ///   Neither implemented                      → "{OwnerAssetName}_{TypeName}"
        ///     generic fallback for any [SOCreate] field on a plain, non-quest SO.
        /// </summary>
        private static string BuildDefaultName(UnityEngine.Object ownerObject, string fieldName, Type concreteType)
        {
            var prefix = (ownerObject as ISOCreateNamingContext)?.NamingPrefix;

            if (ownerObject is ISOCreateVariableNamingContext varContext && !string.IsNullOrEmpty(prefix))
            {
                var varLabel = GetVariableLabel(concreteType);
                return $"{prefix}_Objective{varLabel}_{varContext.VariableShortName}";
            }

            if (!string.IsNullOrEmpty(prefix))
                return $"{prefix}_{fieldName}_{concreteType.Name}";

            return $"{ownerObject.name}_{concreteType.Name}";
        }

        private static string GetVariableLabel(Type type)
        {
            var attr = type.GetCustomAttributes(typeof(SOVariableLabelAttribute), false)
                .FirstOrDefault() as SOVariableLabelAttribute;
            return attr?.Label ?? type.Name;
        }

        private static void AssignToPath(UnityEngine.Object ownerObject, string propertyPath, ScriptableObject instance)
        {
            var so   = new SerializedObject(ownerObject);
            var prop = so.FindProperty(propertyPath);

            if (prop == null || prop.propertyType != SerializedPropertyType.ObjectReference)
            {
                Debug.LogWarning(
                    $"[SOCreate] Field '{propertyPath}' on '{ownerObject.name}' no longer exists — " +
                    "created asset was NOT assigned. Find it manually at the target folder.");
                EditorGUIUtility.PingObject(instance);
                return;
            }

            if (prop.objectReferenceValue != null)
            {
                Debug.LogWarning(
                    $"[SOCreate] Field '{propertyPath}' on '{ownerObject.name}' was already assigned " +
                    "while the Create prompt was open. New asset created but not attached.");
                EditorGUIUtility.PingObject(instance);
                return;
            }

            prop.objectReferenceValue = instance;
            so.ApplyModifiedProperties();
            EditorGUIUtility.PingObject(instance);
        }
        
        private static void AssignToProperty(SerializedProperty property, ScriptableObject instance)
        {
            property.objectReferenceValue = instance;
            property.serializedObject.ApplyModifiedProperties();
            EditorGUIUtility.PingObject(instance);
        }

        private static string ResolveFolder(UnityEngine.Object owner)
        {
            var ownerPath = AssetDatabase.GetAssetPath(owner);
            return string.IsNullOrEmpty(ownerPath)
                ? "Assets"
                : Path.GetDirectoryName(ownerPath)?.Replace('\\', '/') ?? "Assets";
        }

        // ── Cache helpers ─────────────────────────────────────────────────────

        private SerializedObject GetOrCreateSO(SerializedProperty property)
        {
            var target = property.objectReferenceValue;
            if (target == null) return null;

            // Key is owner instance ID + property path — unique per field per asset,
            // handles array slots (e.g. Objectives.Array.data[2]) correctly.
            var key = $"{property.serializedObject.targetObject.GetInstanceID()}_{property.propertyPath}";

            if (soCache.TryGetValue(key, out var cached))
            {
                // Invalidate if the reference was swapped out since last repaint.
                if (cached != null && cached.targetObject == target)
                    return cached;

                cached?.Dispose();
                soCache.Remove(key);
            }

            var so = new SerializedObject(target);
            soCache[key] = so;
            return so;
        }

        // ── Foldout state helpers ─────────────────────────────────────────────

        private static string StateKey(SerializedProperty property)
            => $"{property.serializedObject.targetObject.GetInstanceID()}_{property.propertyPath}";

        private static bool GetFoldoutOpen(SerializedProperty property)
            => foldoutStates.TryGetValue(StateKey(property), out var open) && open;

        private static void SetFoldoutOpen(SerializedProperty property, bool open)
            => foldoutStates[StateKey(property)] = open;

        // ── Field type resolution ─────────────────────────────────────────────

        private Type ResolveFieldType()
        {
            var type = fieldInfo.FieldType;
            if (type.IsArray)       return type.GetElementType();
            if (type.IsGenericType) return type.GetGenericArguments().FirstOrDefault();
            return type;
        }
    }
}