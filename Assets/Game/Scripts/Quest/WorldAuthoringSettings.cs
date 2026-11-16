using UnityEngine;
using SOAP;

/// <summary>
/// Everything the World Authoring window needs that is the same for every
/// placement. Create one asset, assign the dependencies, then press
/// "Create Default Prefabs and Templates" in the window to fill in the rest.
/// </summary>
[CreateAssetMenu(fileName = "WorldAuthoringSettings", menuName = "SOAP/Editor/World Authoring Settings")]
public class WorldAuthoringSettings : ScriptableObject
{
    [Header("Dependencies")]
    [Tooltip("Channel wired into every placed zone, pickup and enemy.")]
    public GameChannel DefaultQuestChannel;

    public QuestGiverRegistry Registry;

    [Tooltip("The player's backpack, wired into every placed pickup.")]
    public InventorySO Inventory;

    [Tooltip("New items created in the window are added here automatically.")]
    public ItemDatabaseSO ItemDatabase;

    [Tooltip("Prefab enemies spawn their loot from. Filled in by the default generator.")]
    public ItemPickup WorldPickupPrefab;

    [Header("Templates")]
    [Tooltip("Used by the Enemies tab.")]
    public PlacementTemplateSO EnemyTemplate;

    [Tooltip("Used by the Items tab to place an item in the world.")]
    public PlacementTemplateSO ItemPickupTemplate;

    [Tooltip("Everything offered in the Placement tab's dropdown.")]
    public PlacementTemplateSO[] Templates;

    [Header("Folders")]
    public string ItemRootFolder = "Assets/Game/Items";
    public string EnemyRootFolder = "Assets/Game/Enemies";
    public string LootRootFolder = "Assets/Game/Loot";
    public string TemplateRootFolder = "Assets/Game/Placement";

    private void OnValidate()
    {
        if (DefaultQuestChannel == null)
            Debug.LogWarning("[WorldAuthoringSettings] DefaultQuestChannel is unassigned.", this);
        if (Inventory == null)
            Debug.LogWarning("[WorldAuthoringSettings] Inventory is unassigned.", this);
    }
}
