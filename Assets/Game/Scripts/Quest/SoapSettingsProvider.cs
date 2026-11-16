using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace SOAP
{
    public static class SoapSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateUnifiedSettingsProvider()
        {
            // 1. Define the path where the tab will appear in Project Settings
            var provider = new SettingsProvider("Project/SOAP Authoring", SettingsScope.Project)
            {
                label = "SOAP Authoring",
                guiHandler = (searchContext) =>
                {
                    // 2. Fetch or auto-create the assets
                    var questSettings = GetOrCreateQuestSettings();
                    var npcSettings = GetOrCreateNpcSettings();

                    // 3. Draw Quest Settings (Uses your existing Custom Editor automatically!)
                    EditorGUILayout.LabelField("Quest Authoring", EditorStyles.boldLabel);
                    var questEditor = Editor.CreateEditor(questSettings);
                    questEditor.OnInspectorGUI(); 

                    EditorGUILayout.Space(20);
                    DrawUILine(Color.gray);
                    EditorGUILayout.Space(10);

                    // 4. Draw NPC Settings
                    EditorGUILayout.LabelField("NPC Setup", EditorStyles.boldLabel);
                    var npcEditor = Editor.CreateEditor(npcSettings);
                    npcEditor.OnInspectorGUI();
                },
            
                // Allow users to find this tab using Unity's settings search bar
                keywords = new HashSet<string>(new[] { "Quest", "NPC", "SOAP", "Authoring", "Settings", "Registry" })
            };

            return provider;
        }

        // ── Asset Retrieval & Auto-Creation Helpers ───────────────────────────

        private static QuestAuthoringSettings GetOrCreateQuestSettings()
        {
            var guids = AssetDatabase.FindAssets("t:QuestAuthoringSettings");
            if (guids.Length > 0) 
                return AssetDatabase.LoadAssetAtPath<QuestAuthoringSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
        
            // Matches the fallback generation logic from QuestAuthoringWindow
            var settings = ScriptableObject.CreateInstance<QuestAuthoringSettings>();
            EnsureFolder("Assets/Editor");
            AssetDatabase.CreateAsset(settings, "Assets/Editor/QuestAuthoringSettings.asset");
            AssetDatabase.SaveAssets();
            return settings;
        }

        private static NpcAuthoringSettings GetOrCreateNpcSettings()
        {
            var guids = AssetDatabase.FindAssets("t:NpcAuthoringSettings");
            if (guids.Length > 0) 
                return AssetDatabase.LoadAssetAtPath<NpcAuthoringSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
        
            var settings = ScriptableObject.CreateInstance<NpcAuthoringSettings>();
            EnsureFolder("Assets/Editor");
            AssetDatabase.CreateAsset(settings, "Assets/Editor/NpcAuthoringSettings.asset");
            AssetDatabase.SaveAssets();
            return settings;
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
                var name = System.IO.Path.GetFileName(path);
                if (parent != null) AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void DrawUILine(Color color, int thickness = 1, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }
    }
}