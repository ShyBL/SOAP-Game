using UnityEditor;
using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Adds a live preview beneath the naming template fields — typing a
    /// template renders an example filename underneath using placeholder
    /// sample values, so the effect of a change is visible immediately
    /// rather than only discoverable next time something gets created.
    /// Purely cosmetic — QuestAuthoringSettings works fine with Unity's
    /// default inspector if this is skipped.
    /// </summary>
    [CustomEditor(typeof(QuestAuthoringSettings))]
    public class QuestAuthoringSettingsEditor : Editor
    {
        private const string SampleQuestId  = "MQ_001";
        private const string SampleShortName = "SurvivorRescued";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var settings = (QuestAuthoringSettings)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            string questExample = QuestAuthoringSettings.ApplyTemplate(
                settings.QuestDefinitionNameTemplate,
                ("QuestId", SampleQuestId));

            string objectiveExample = QuestAuthoringSettings.ApplyTemplate(
                settings.ObjectiveNameTemplate,
                ("QuestId", SampleQuestId), ("ShortName", SampleShortName));

            EditorGUILayout.LabelField($"Quest:     {questExample}.asset");
            EditorGUILayout.LabelField($"Objective: {objectiveExample}.asset");
        }
    }
}