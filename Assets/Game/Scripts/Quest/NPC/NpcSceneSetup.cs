using UnityEditor;
using UnityEngine;
using SOAP;

public static partial class NpcSceneSetup
{
    public static void SpawnNpcInScene(NpcQuestGiver npc, NpcAuthoringSettings settings)
    {
        if (settings.MasterNpcPrefab == null)
        {
            Debug.LogError("[NpcSceneSetup] Master NPC Prefab is missing in Settings.");
            return;
        }

        // 1. Instantiate the prefab while keeping the connection intact
        var go = (GameObject)PrefabUtility.InstantiatePrefab(settings.MasterNpcPrefab);
        Undo.RegisterCreatedObjectUndo(go, "Spawn NPC");

        // 2. Name it and put it in a clean container
        go.name = string.IsNullOrEmpty(npc.DisplayName) ? npc.NpcId : npc.DisplayName;

        var container = GameObject.Find("NPCs");
        if (container == null)
        {
            container = new GameObject("NPCs");
            Undo.RegisterCreatedObjectUndo(container, "Create NPCs Container");
        }
        Undo.SetTransformParent(go.transform, container.transform, "Parent NPC");

        // 3. Wire the trigger zones using SerializedObject (bypasses protected fields)
        var zones = go.GetComponentsInChildren<QuestGiverZone>(true);
        foreach (var zone in zones)
        {
            var serializedZone = new SerializedObject(zone);
            serializedZone.FindProperty("npcId").stringValue = npc.NpcId;
            serializedZone.FindProperty("questChannel").objectReferenceValue = settings.DefaultQuestChannel;
            serializedZone.FindProperty("registry").objectReferenceValue = settings.Registry;
            serializedZone.ApplyModifiedProperties();
        }

        // 3b. Wire any map/compass markers the same way
        var mapMarkers = go.GetComponentsInChildren<NpcMapMarker>(true);
        foreach (var marker in mapMarkers)
        {
            var serializedMarker = new SerializedObject(marker);
            serializedMarker.FindProperty("npcId").stringValue = npc.NpcId;
            serializedMarker.FindProperty("registry").objectReferenceValue = settings.Registry;
            serializedMarker.ApplyModifiedProperties();
        }

        // 4. Focus the new object in the hierarchy
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
        Debug.Log($"[NpcSceneSetup] Spawned {go.name} into the scene successfully.");
    }
    
}

