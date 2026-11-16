using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Shared logic for picking a concrete subtype of an abstract
    /// ScriptableObject and creating it as a project asset. Originally lived
    /// inline inside SOCreateDrawer; extracted so QuestAuthoringWindow's
    /// "+ Add Objective" button can use the exact same subtype-picker
    /// behavior instead of duplicating the TypeCache/GenericMenu logic.
    ///
    /// Deliberately narrow scope: this class picks a subtype and creates the
    /// asset. It has no opinion about what the caller does with the result —
    /// assign it to a field (SOCreateDrawer), append it to an array and
    /// refresh some UI (QuestAuthoringWindow), or anything else.
    /// </summary>
    public static class ScriptableObjectSubtypeMenu
    {
        /// <summary>
        /// Shows a context menu listing every concrete subtype of
        /// abstractType found in the project. On selection, creates that
        /// subtype as an asset in the given folder (default auto-generated
        /// name) and invokes onCreated with the new instance. Used by
        /// SOCreateDrawer, where no naming prompt is wanted — the generic
        /// {OwnerName}_{FieldName} default applies.
        /// </summary>
        public static void Show(Type abstractType, string folder, Action<ScriptableObject> onCreated)
        {
            var subtypes = GetConcreteSubtypes(abstractType);
            if (subtypes.Count == 0) return;

            var menu = new GenericMenu();

            foreach (var subtype in subtypes)
            {
                var captured = subtype;
                menu.AddItem(new GUIContent(NiceTypeName(captured)), false,
                    () => onCreated(CreateAsset(captured, folder)));
            }

            menu.ShowAsContext();
        }

        /// <summary>
        /// Shows the same subtype-picker menu, but only reports which type
        /// was picked — creates nothing itself. Used by QuestAuthoringWindow,
        /// which needs to prompt for a name (via EditorInputPrompt) between
        /// the type pick and actually creating the asset, something Show()
        /// doesn't support and shouldn't be changed to — SOCreateDrawer's
        /// existing flow needs no naming prompt at all.
        /// </summary>
        public static void PickType(Type abstractType, Action<Type> onPicked)
        {
            var subtypes = GetConcreteSubtypes(abstractType);
            if (subtypes.Count == 0) return;

            var menu = new GenericMenu();

            foreach (var subtype in subtypes)
            {
                var captured = subtype;
                menu.AddItem(new GUIContent(NiceTypeName(captured)), false, () => onPicked(captured));
            }

            menu.ShowAsContext();
        }

        /// <summary>
        /// Creates a new instance of type as a project asset inside folder.
        /// If fileName is omitted, falls back to the original New_{TypeName}
        /// default. Saves and refreshes the AssetDatabase immediately so the
        /// asset is usable right away.
        /// </summary>
        public static ScriptableObject CreateAsset(Type type, string folder, string fileName = null)
        {
            var name = string.IsNullOrEmpty(fileName) ? $"New_{type.Name}" : fileName;
            var path     = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{name}.asset");
            var instance = ScriptableObject.CreateInstance(type);

            AssetDatabase.CreateAsset(instance, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return instance;
        }

        private static System.Collections.Generic.List<Type> GetConcreteSubtypes(Type abstractType)
        {
            var subtypes = TypeCache.GetTypesDerivedFrom(abstractType)
                .Where(t => !t.IsAbstract)
                .OrderBy(t => t.Name)
                .ToList();

            if (subtypes.Count == 0)
                Debug.LogWarning($"[ScriptableObjectSubtypeMenu] No concrete subtypes of '{abstractType.Name}' found.");

            return subtypes;
        }

        /// <summary>
        /// Strips a trailing "SO" suffix before nicifying so that
        /// "CountUpObjectiveSO" displays as "Count Up Objective" rather than
        /// "Count Up Objective S O".
        /// </summary>
        public static string NiceTypeName(Type type)
        {
            var name = type.Name.EndsWith("SO")
                ? type.Name.Substring(0, type.Name.Length - 2)
                : type.Name;
            return ObjectNames.NicifyVariableName(name);
        }
    }
}
