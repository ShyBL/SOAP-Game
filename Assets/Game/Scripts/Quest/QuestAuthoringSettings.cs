using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Project-wide settings for QuestAuthoringWindow — one asset per
    /// project, same singleton-style pattern as QuestGiverRegistry.
    ///
    /// Owns only what QuestAuthoringWindow itself directly creates via its
    /// own dedicated buttons: the quest folder, and the naming templates for
    /// the Quest asset and Objective assets. Does NOT control
    /// SOCreateDrawer's generic {OwnerName}_{FieldName} fallback naming used
    /// for Counter/Flag/state variables — that stays a simple, framework-wide
    /// default, not something quest-specific settings reach into.
    ///
    /// Found via AssetDatabase.FindAssets("t:QuestAuthoringSettings") rather
    /// than a fixed path — auto-created with these defaults the first time
    /// QuestAuthoringWindow opens and finds none.
    /// </summary>
    [CreateAssetMenu(fileName = "QuestAuthoringSettings", menuName = "SOAP/Quest/Authoring Settings")]
    public class QuestAuthoringSettings : ScriptableObject
    {
        [Header("Folder")]
        [Tooltip("Root folder new quests are created in. Auto-created if it doesn't exist yet.")]
        public string QuestRootFolder = "Assets/Game/Quests";

        [Header("Naming Templates")]
        [Tooltip("Placeholders: {QuestId}")]
        public string QuestDefinitionNameTemplate = "{QuestId}_QuestDefinition";

        [Tooltip("Placeholders: {QuestId}, {ShortName}")]
        public string ObjectiveNameTemplate = "{QuestId}_QuestObjective_{ShortName}";

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(QuestRootFolder))
                Debug.LogWarning($"[QuestAuthoringSettings] '{name}': QuestRootFolder is empty.", this);

            if (string.IsNullOrEmpty(QuestDefinitionNameTemplate) || !QuestDefinitionNameTemplate.Contains("{QuestId}"))
                Debug.LogWarning($"[QuestAuthoringSettings] '{name}': QuestDefinitionNameTemplate should contain " +
                                 "{QuestId} — without it, every quest gets the same filename and silently collides.", this);

            if (string.IsNullOrEmpty(ObjectiveNameTemplate) || !ObjectiveNameTemplate.Contains("{ShortName}"))
                Debug.LogWarning($"[QuestAuthoringSettings] '{name}': ObjectiveNameTemplate should contain " +
                                 "{ShortName} — without it, every objective in a quest gets the same filename and silently collides.", this);
        }

        /// <summary>
        /// Applies a small, fixed set of {Token} substitutions to a template
        /// string. Not a general templating engine — there are only ever two
        /// known tokens across both templates.
        /// </summary>
        public static string ApplyTemplate(string template, params (string token, string value)[] substitutions)
        {
            foreach (var (token, value) in substitutions)
                template = template.Replace("{" + token + "}", value);
            return template;
        }
    }
}
