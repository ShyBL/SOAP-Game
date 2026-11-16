using System;
using UnityEngine;

/// <summary>
/// Where a binding's value comes from when an object is placed. Settings-level
/// sources (channel, registry, inventory, pickup prefab) are identical for
/// every placement; the rest are chosen in the World Authoring window.
/// </summary>
public enum PlacementValueSource
{
    QuestChannel,
    QuestGiverRegistry,
    Inventory,
    PickupPrefab,
    EventId,
    Item,
    ItemCount,
    LootTable,
    Quest,
    UniqueId
}

/// <summary>"On component X, set serialized field Y from source Z."</summary>
[Serializable]
public struct PlacementBinding
{
    [Tooltip("Component class name, e.g. SurvivorRescueZone.")]
    public string ComponentType;

    [Tooltip("Serialized field name on that component, e.g. eventId.")]
    public string FieldName;

    public PlacementValueSource Source;
}

/// <summary>
/// Describes one kind of placeable object: which prefab to instantiate,
/// which components it must have, and how each component's fields are filled
/// in. The window and PlacementWiring know nothing about specific components —
/// supporting a new placeable (or a renamed field) is an asset edit, not a
/// code change.
/// </summary>
[CreateAssetMenu(fileName = "New_PlacementTemplate", menuName = "SOAP/Editor/Placement Template")]
public class PlacementTemplateSO : ScriptableObject
{
    public string DisplayName;

    [Tooltip("Prefab to instantiate (kept linked). May be empty when the caller supplies one, " +
             "e.g. an EnemyDefinition's prefab.")]
    public GameObject Prefab;

    [Tooltip("Scene object the instance is parented under; created if missing.")]
    public string Container = "Placed";

    [Tooltip("Component class names added to the instance if it does not already have them.")]
    public string[] EnsureComponents;

    public PlacementBinding[] Bindings;

    /// <summary>True when any binding reads the given source — drives which inputs the window shows.</summary>
    public bool Uses(PlacementValueSource source)
    {
        if (Bindings == null) return false;

        for (int i = 0; i < Bindings.Length; i++)
            if (Bindings[i].Source == source)
                return true;

        return false;
    }
}
