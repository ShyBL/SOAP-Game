using UnityEngine;
using SOAP;

[CreateAssetMenu(fileName = "NpcAuthoringSettings", menuName = "SOAP/Editor/Npc Authoring Settings")]
public class NpcAuthoringSettings : ScriptableObject
{
    [Header("File Paths")]
    public string NpcRootFolder = "Assets/Game/NPCs";
    public string NpcQuestGiverNameTemplate = "{NpcId}_NpcQuestGiver";

    [Header("Dependencies")]
    [Tooltip("The project-wide registry. New NPCs will be automatically added here.")]
    public QuestGiverRegistry Registry;
    
    [Tooltip("The channel automatically wired into spawned NPC zones.")]
    public GameChannel DefaultQuestChannel;

    [Header("Spawning")]
    [Tooltip("The fully assembled Master NPC Prefab containing cameras, UI, and zones.")]
    public GameObject MasterNpcPrefab;
    
    private void OnValidate()
    {
        if (Registry == null) Debug.LogWarning("[NpcAuthoringSettings] Registry is unassigned.");
        if (MasterNpcPrefab == null) Debug.LogWarning("[NpcAuthoringSettings] MasterNpcPrefab is unassigned.");
    }
}