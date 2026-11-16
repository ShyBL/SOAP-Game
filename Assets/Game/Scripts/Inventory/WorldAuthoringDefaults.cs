using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using SOAP;

/// <summary>
/// Builds the starting set of placeable prefabs and templates so nothing is
/// assembled by hand. Safe to run repeatedly: anything that already exists is
/// kept (including your edits to the prefabs), only missing pieces are added.
///
/// The generated prefabs are deliberately plain — a trigger collider plus the
/// gameplay components. Add your visuals (survivor model, scroll mesh, map
/// icon) to the PREFAB once and every placed instance gets them.
/// </summary>
public static class WorldAuthoringDefaults
{
    public static void CreateDefaults(WorldAuthoringSettings settings)
    {
        if (settings == null) return;

        string templateFolder = settings.TemplateRootFolder;
        string prefabFolder = templateFolder + "/Prefabs";
        EditorAssetUtility.EnsureFolder(prefabFolder);

        // ── Prefabs ───────────────────────────────────────────────────────────

        GameObject rescuePrefab = BuildPrefab(prefabFolder, "RescueZone", 2f, null,
            typeof(SurvivorRescueZone), typeof(MapMarker));

        GameObject pickupPrefab = BuildPrefab(prefabFolder, "ItemPickup", 1.5f,
            go => SetObject(go, typeof(ItemPickup), "inventory", settings.Inventory),
            typeof(ItemPickup));

        GameObject scrollPrefab = BuildPrefab(prefabFolder, "QuestScroll", 1.5f, go =>
            {
                SetObject(go, typeof(ItemPickup), "inventory", settings.Inventory);
                SetBool(go, typeof(QuestGrantPickup), "grantOnTrigger", false);
                WireScrollGrant(go);
            },
            typeof(ItemPickup), typeof(QuestGrantPickup));

        GameObject discoveryPrefab = BuildPrefab(prefabFolder, "DiscoveryZone", 6f,
            go => SetBool(go, typeof(NpcDiscoveryZone), "doOnce", true),
            typeof(NpcDiscoveryZone));

        GameObject echoPrefab = BuildPrefab(prefabFolder, "EchoZone", 3f,
            go => SetBool(go, typeof(EchoTriggerZone), "doOnce", true),
            typeof(EchoTriggerZone));

        // ── Templates ─────────────────────────────────────────────────────────

        PlacementBinding channel = Bind(nameof(SurvivorRescueZone), "questChannel", PlacementValueSource.QuestChannel);

        PlacementTemplateSO rescue = CreateTemplate(templateFolder, "Rescue Zone", rescuePrefab, "RescueZones", null,
            channel,
            Bind(nameof(SurvivorRescueZone), "eventId", PlacementValueSource.EventId),
            Bind(nameof(SurvivorRescueZone), "persistentId", PlacementValueSource.UniqueId));

        PlacementTemplateSO itemPickup = CreateTemplate(templateFolder, "Item Pickup", pickupPrefab, "Pickups", null,
            Bind(nameof(ItemPickup), "inventory", PlacementValueSource.Inventory),
            Bind(nameof(ItemPickup), "item", PlacementValueSource.Item),
            Bind(nameof(ItemPickup), "count", PlacementValueSource.ItemCount),
            Bind(nameof(ItemPickup), "persistentId", PlacementValueSource.UniqueId));

        PlacementTemplateSO questItem = CreateTemplate(templateFolder, "Quest Item", pickupPrefab, "Pickups", null,
            Bind(nameof(ItemPickup), "inventory", PlacementValueSource.Inventory),
            Bind(nameof(ItemPickup), "item", PlacementValueSource.Item),
            Bind(nameof(ItemPickup), "persistentId", PlacementValueSource.UniqueId),
            Bind(nameof(ItemPickup), "questChannel", PlacementValueSource.QuestChannel),
            Bind(nameof(ItemPickup), "questEventId", PlacementValueSource.EventId));

        PlacementTemplateSO questScroll = CreateTemplate(templateFolder, "Quest Scroll", scrollPrefab, "Pickups", null,
            Bind(nameof(ItemPickup), "inventory", PlacementValueSource.Inventory),
            Bind(nameof(ItemPickup), "item", PlacementValueSource.Item),
            Bind(nameof(ItemPickup), "persistentId", PlacementValueSource.UniqueId),
            Bind(nameof(QuestGrantPickup), "questChannel", PlacementValueSource.QuestChannel),
            Bind(nameof(QuestGrantPickup), "grantedQuest", PlacementValueSource.Quest));

        PlacementTemplateSO discovery = CreateTemplate(templateFolder, "Discovery Zone", discoveryPrefab, "Discovery", null,
            Bind(nameof(NpcDiscoveryZone), "questChannel", PlacementValueSource.QuestChannel),
            Bind(nameof(NpcDiscoveryZone), "eventId", PlacementValueSource.EventId));

        PlacementTemplateSO echo = CreateTemplate(templateFolder, "Echo Zone", echoPrefab, "Echoes", null,
            Bind(nameof(EchoTriggerZone), "questChannel", PlacementValueSource.QuestChannel),
            Bind(nameof(EchoTriggerZone), "eventId", PlacementValueSource.EventId));

        // The enemy prefab comes from each EnemyDefinitionSO, so this template has none.
        PlacementTemplateSO enemy = CreateTemplate(templateFolder, "Enemy", null, "Enemies",
            new[] { nameof(LootDropper), nameof(EnemyDeathReporter), nameof(EnemyDefeatedRelay) },
            Bind(nameof(LootDropper), "lootTable", PlacementValueSource.LootTable),
            Bind(nameof(LootDropper), "pickupPrefab", PlacementValueSource.PickupPrefab),
            Bind(nameof(EnemyDeathReporter), "questChannel", PlacementValueSource.QuestChannel),
            Bind(nameof(EnemyDeathReporter), "eventId", PlacementValueSource.EventId));

        // ── Settings ──────────────────────────────────────────────────────────

        settings.WorldPickupPrefab = pickupPrefab != null ? pickupPrefab.GetComponent<ItemPickup>() : settings.WorldPickupPrefab;
        settings.EnemyTemplate = enemy;
        settings.ItemPickupTemplate = itemPickup;
        settings.Templates = new[] { rescue, itemPickup, questItem, questScroll, discovery, echo };

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log("[WorldAuthoringDefaults] Default prefabs and templates are ready.", settings);
    }

    // ── Prefab building ───────────────────────────────────────────────────────

    /// <summary>
    /// Creates a prefab with a trigger SphereCollider and the given components.
    /// If the prefab already exists it is returned untouched.
    /// </summary>
    private static GameObject BuildPrefab(string folder, string prefabName, float triggerRadius,
        Action<GameObject> configure, params Type[] components)
    {
        string path = $"{folder}/{prefabName}.prefab";

        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var go = new GameObject(prefabName);

        var collider = go.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = triggerRadius;

        foreach (Type type in components)
            go.AddComponent(type);

        configure?.Invoke(go);

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, path);
        UnityEngine.Object.DestroyImmediate(go);
        return saved;
    }

    private static void SetBool(GameObject go, Type componentType, string field, bool value)
    {
        Component component = go.GetComponent(componentType);
        if (component == null) return;

        var serialized = new SerializedObject(component);
        SerializedProperty property = serialized.FindProperty(field);
        if (property == null) return;

        property.boolValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObject(GameObject go, Type componentType, string field, UnityEngine.Object value)
    {
        if (value == null) return;

        Component component = go.GetComponent(componentType);
        if (component == null) return;

        var serialized = new SerializedObject(component);
        SerializedProperty property = serialized.FindProperty(field);
        if (property == null) return;

        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// The scroll is stored in the backpack AND grants its quest: wire
    /// ItemPickup.onCollected to QuestGrantPickup.Grant as a persistent listener.
    /// </summary>
    private static void WireScrollGrant(GameObject go)
    {
        ItemPickup pickup = go.GetComponent<ItemPickup>();
        QuestGrantPickup grant = go.GetComponent<QuestGrantPickup>();
        if (pickup == null || grant == null) return;

        FieldInfo field = typeof(ItemPickup).GetField("onCollected", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null)
        {
            Debug.LogWarning("[WorldAuthoringDefaults] ItemPickup.onCollected not found — wire the scroll's Grant call by hand.");
            return;
        }

        var collected = (UnityEvent)field.GetValue(pickup);
        UnityEventTools.AddVoidPersistentListener(collected, grant.Grant);
        EditorUtility.SetDirty(pickup);
    }

    // ── Template building ─────────────────────────────────────────────────────

    private static PlacementTemplateSO CreateTemplate(string folder, string displayName, GameObject prefab,
        string container, string[] ensureComponents, params PlacementBinding[] bindings)
    {
        string path = $"{folder}/{displayName.Replace(" ", string.Empty)}Template.asset";

        var existing = AssetDatabase.LoadAssetAtPath<PlacementTemplateSO>(path);
        if (existing != null) return existing;

        var template = ScriptableObject.CreateInstance<PlacementTemplateSO>();
        template.DisplayName = displayName;
        template.Prefab = prefab;
        template.Container = container;
        template.EnsureComponents = ensureComponents ?? new string[0];
        template.Bindings = bindings;

        AssetDatabase.CreateAsset(template, path);
        return template;
    }

    private static PlacementBinding Bind(string component, string field, PlacementValueSource source)
    {
        return new PlacementBinding { ComponentType = component, FieldName = field, Source = source };
    }
}