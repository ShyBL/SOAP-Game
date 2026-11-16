using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SOAP;

/// <summary>The per-placement choices the window collects; settings supply the shared sources.</summary>
public class PlacementContext
{
    public WorldAuthoringSettings Settings;
    public string Name;
    public GameObject PrefabOverride;
    public string EventId;
    public ItemDefinitionSO Item;
    public int ItemCount = 1;
    public LootTableSO LootTable;
    public QuestDefinitionSO Quest;
}

/// <summary>
/// Generalizes what NpcSceneSetup does for NPCs: instantiate a prefab, then
/// fill in component fields through SerializedObject (which reaches private
/// [SerializeField] fields). Which components and fields is described by a
/// PlacementTemplateSO, so this class contains no knowledge of any specific
/// gameplay component.
/// </summary>
public static class PlacementWiring
{
    public static GameObject Place(PlacementTemplateSO template, PlacementContext context, Vector3 position)
    {
        if (template == null || context == null || context.Settings == null) return null;

        GameObject prefab = context.PrefabOverride != null ? context.PrefabOverride : template.Prefab;
        if (prefab == null)
        {
            Debug.LogError($"[PlacementWiring] Template '{template.DisplayName}' has no prefab and none was supplied.", template);
            return null;
        }

        GameObject instance = PrefabUtility.IsPartOfPrefabAsset(prefab)
            ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
            : UnityEngine.Object.Instantiate(prefab);

        Undo.RegisterCreatedObjectUndo(instance, $"Place {template.DisplayName}");
        instance.name = string.IsNullOrEmpty(context.Name) ? prefab.name : context.Name;

        ParentUnderContainer(instance, template.Container);
        instance.transform.position = position;

        EnsureComponents(instance, template);
        ApplyBindings(instance, template, context);

        return instance;
    }

    // ── Hierarchy ─────────────────────────────────────────────────────────────

    private static void ParentUnderContainer(GameObject instance, string containerName)
    {
        if (string.IsNullOrEmpty(containerName)) return;

        GameObject container = GameObject.Find(containerName);
        if (container == null)
        {
            container = new GameObject(containerName);
            Undo.RegisterCreatedObjectUndo(container, $"Create {containerName} Container");
        }

        Undo.SetTransformParent(instance.transform, container.transform, "Parent placed object");
    }

    // ── Components ────────────────────────────────────────────────────────────

    private static void EnsureComponents(GameObject instance, PlacementTemplateSO template)
    {
        if (template.EnsureComponents == null) return;

        var typeCache = new Dictionary<string, Type>();

        foreach (string typeName in template.EnsureComponents)
        {
            Type type = ResolveType(typeName, typeCache);
            if (type == null)
            {
                Debug.LogWarning($"[PlacementWiring] Unknown component type '{typeName}' in template '{template.DisplayName}'.", template);
                continue;
            }

            if (instance.GetComponentInChildren(type, true) != null) continue;

            Undo.AddComponent(instance, type);
        }
    }

    private static void ApplyBindings(GameObject instance, PlacementTemplateSO template, PlacementContext context)
    {
        if (template.Bindings == null) return;

        var typeCache = new Dictionary<string, Type>();

        foreach (PlacementBinding binding in template.Bindings)
        {
            Type type = ResolveType(binding.ComponentType, typeCache);
            if (type == null)
            {
                Debug.LogWarning($"[PlacementWiring] Unknown component type '{binding.ComponentType}' in template '{template.DisplayName}'.", template);
                continue;
            }

            Component[] components = instance.GetComponentsInChildren(type, true);
            if (components.Length == 0)
            {
                Debug.LogWarning($"[PlacementWiring] '{instance.name}' has no {binding.ComponentType} to wire '{binding.FieldName}' on.", instance);
                continue;
            }

            object value = Resolve(binding.Source, context);
            if (IsMissing(value))
            {
                Debug.LogWarning($"[PlacementWiring] No value for {binding.Source} — {binding.ComponentType}.{binding.FieldName} " +
                                 $"left unchanged on '{instance.name}'. Fill it in the window or in the settings asset.", instance);
                continue;
            }

            foreach (Component component in components)
            {
                var serialized = new SerializedObject(component);
                SerializedProperty property = serialized.FindProperty(binding.FieldName);

                if (property == null)
                {
                    Debug.LogWarning($"[PlacementWiring] {binding.ComponentType} has no serialized field '{binding.FieldName}'.", component);
                    continue;
                }

                if (!TryAssign(property, value))
                    Debug.LogWarning($"[PlacementWiring] {binding.ComponentType}.{binding.FieldName} is {property.propertyType}; " +
                                     $"cannot assign a {binding.Source} value.", component);

                serialized.ApplyModifiedProperties();
            }
        }
    }

    // ── Values ────────────────────────────────────────────────────────────────

    private static object Resolve(PlacementValueSource source, PlacementContext context)
    {
        WorldAuthoringSettings settings = context.Settings;

        switch (source)
        {
            case PlacementValueSource.QuestChannel:       return settings.DefaultQuestChannel;
            case PlacementValueSource.QuestGiverRegistry: return settings.Registry;
            case PlacementValueSource.Inventory:          return settings.Inventory;
            case PlacementValueSource.PickupPrefab:       return settings.WorldPickupPrefab;
            case PlacementValueSource.EventId:            return context.EventId;
            case PlacementValueSource.Item:               return context.Item;
            case PlacementValueSource.ItemCount:          return context.ItemCount;
            case PlacementValueSource.LootTable:          return context.LootTable;
            case PlacementValueSource.Quest:              return context.Quest;
            case PlacementValueSource.UniqueId:           return Guid.NewGuid().ToString("N");
            default:                                      return null;
        }
    }

    private static bool IsMissing(object value)
    {
        if (value == null) return true;
        if (value is UnityEngine.Object unityObject && unityObject == null) return true;
        if (value is string text && string.IsNullOrEmpty(text)) return true;
        return false;
    }

    private static bool TryAssign(SerializedProperty property, object value)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.String:
                property.stringValue = value as string ?? Convert.ToString(value);
                return true;

            case SerializedPropertyType.Integer:
                property.intValue = Convert.ToInt32(value);
                return true;

            case SerializedPropertyType.ObjectReference:
                if (!(value is UnityEngine.Object unityObject)) return false;
                property.objectReferenceValue = unityObject;
                return true;

            default:
                return false;
        }
    }

    private static Type ResolveType(string typeName, Dictionary<string, Type> cache)
    {
        if (string.IsNullOrEmpty(typeName)) return null;
        if (cache.TryGetValue(typeName, out Type cached)) return cached;

        Type found = null;
        foreach (Type type in TypeCache.GetTypesDerivedFrom<Component>())
        {
            if (type.Name == typeName || type.FullName == typeName)
            {
                found = type;
                break;
            }
        }

        cache[typeName] = found;
        return found;
    }
}
