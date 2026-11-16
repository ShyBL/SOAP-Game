using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using SOAP;

/// <summary>
/// One window for everything placed in the world and everything that drops:
///
///   Placement   → pick a template (rescue zone, scroll, echo zone, ...), pick
///                 the objective/item/quest it needs, press Place. Event ids,
///                 channels, inventory and persistent ids are filled in for you.
///   Enemies     → create enemy definitions (with their loot table) and place
///                 them fully wired.
///   Items       → create misc/armor/weapon items; new items are added to the
///                 item database automatically. Place any item as a pickup.
///   Loot Tables → create and edit drop tables.
///
/// Quest rewards live on the quest asset itself (QuestDefinitionSO.RewardItems).
/// Built in code rather than UXML so there is no layout asset to assign.
/// </summary>
public class WorldAuthoringWindow : EditorWindow
{
    private WorldAuthoringSettings settings;
    private IntegerField countField;
    private readonly List<VisualElement> panes = new List<VisualElement>();

    // Placement pane
    private DropdownField templateDropdown;
    private ObjectField prefabField;
    private ObjectField objectiveField;
    private TextField eventIdField;
    private ObjectField itemField;
    private IntegerField itemCountField;
    private ObjectField lootField;
    private ObjectField questField;
    private VisualElement prefabRow;
    private VisualElement eventRow;
    private VisualElement itemRow;
    private VisualElement itemCountRow;
    private VisualElement lootRow;
    private VisualElement questRow;

    // Asset panes
    private AssetPane<EnemyDefinitionSO> enemiesPane;
    private AssetPane<ItemDefinitionSO> itemsPane;
    private AssetPane<LootTableSO> lootPane;

    [MenuItem("SOAP/World Authoring Window")]
    public static void ShowWindow()
    {
        var window = GetWindow<WorldAuthoringWindow>("World Authoring");
        window.minSize = new Vector2(760, 520);
    }

    private void CreateGUI()
    {
        Build();
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    private void Build()
    {
        rootVisualElement.Clear();
        panes.Clear();

        LoadSettings();
        if (settings == null)
        {
            rootVisualElement.Add(new Label("No WorldAuthoringSettings asset found."));
            rootVisualElement.Add(new Button(CreateSettingsAsset) { text = "Create Settings Asset" });
            return;
        }

        rootVisualElement.style.paddingTop = 8;
        rootVisualElement.style.paddingLeft = 8;
        rootVisualElement.style.paddingRight = 8;
        rootVisualElement.style.paddingBottom = 8;

        // Header: default-prefab generator + shared placement count
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.marginBottom = 8;

        if (settings.EnemyTemplate == null || settings.ItemPickupTemplate == null)
        {
            var generate = new Button(() =>
            {
                WorldAuthoringDefaults.CreateDefaults(settings);
                Build();
            }) { text = "Create Default Prefabs and Templates" };
            header.Add(generate);
        }

        countField = new IntegerField("Place how many") { value = 1 };
        countField.style.minWidth = 220;
        header.Add(countField);
        rootVisualElement.Add(header);

        // Tabs
        var tabRow = new VisualElement();
        tabRow.style.flexDirection = FlexDirection.Row;
        tabRow.style.marginBottom = 6;
        rootVisualElement.Add(tabRow);

        var content = new VisualElement();
        content.style.flexGrow = 1;
        rootVisualElement.Add(content);

        AddTab(tabRow, content, "Placement", BuildPlacementPane());
        AddTab(tabRow, content, "Enemies", BuildEnemiesPane());
        AddTab(tabRow, content, "Items", BuildItemsPane());
        AddTab(tabRow, content, "Loot Tables", BuildLootPane());

        ShowPane(0);
    }

    private void AddTab(VisualElement tabRow, VisualElement content, string title, VisualElement pane)
    {
        int index = panes.Count;
        panes.Add(pane);
        content.Add(pane);
        tabRow.Add(new Button(() => ShowPane(index)) { text = title });
    }

    private void ShowPane(int index)
    {
        for (int i = 0; i < panes.Count; i++)
            panes[i].style.display = i == index ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void LoadSettings()
    {
        settings = null;

        string[] guids = AssetDatabase.FindAssets("t:WorldAuthoringSettings");
        if (guids.Length > 0)
            settings = AssetDatabase.LoadAssetAtPath<WorldAuthoringSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    private void CreateSettingsAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject("Create World Authoring Settings",
            "WorldAuthoringSettings", "asset", "Choose where to save the settings asset.");

        if (string.IsNullOrEmpty(path)) return;

        AssetDatabase.CreateAsset(CreateInstance<WorldAuthoringSettings>(), path);
        AssetDatabase.SaveAssets();
        Build();
    }

    // ── Placement pane ────────────────────────────────────────────────────────

    private VisualElement BuildPlacementPane()
    {
        var pane = new ScrollView();

        var names = new List<string>();
        if (settings.Templates != null)
            foreach (PlacementTemplateSO template in settings.Templates)
                names.Add(template != null ? template.DisplayName : "(missing template)");

        if (names.Count == 0)
        {
            pane.Add(new Label("No templates yet. Press \"Create Default Prefabs and Templates\" above."));
            return pane;
        }

        templateDropdown = new DropdownField("Template", names, 0);
        templateDropdown.RegisterValueChangedCallback(evt => RefreshPlacementInputs());
        pane.Add(templateDropdown);

        prefabField = MakeObjectField<GameObject>("Prefab", pane, out prefabRow);
        eventRow = new VisualElement();
        pane.Add(eventRow);
        objectiveField = MakeObjectField<QuestObjectiveSO>("Objective (event id)", eventRow, out _);
        eventIdField = new TextField("...or custom event id");
        eventRow.Add(eventIdField);
        itemField = MakeObjectField<ItemDefinitionSO>("Item", pane, out itemRow);
        itemCountField = new IntegerField("Stack count") { value = 1 };
        itemCountRow = itemCountField;
        pane.Add(itemCountField);
        lootField = MakeObjectField<LootTableSO>("Loot table", pane, out lootRow);
        questField = MakeObjectField<QuestDefinitionSO>("Quest", pane, out questRow);

        var place = new Button(PlaceFromPlacementPane) { text = "Place in Scene" };
        place.style.marginTop = 10;
        place.style.height = 32;
        pane.Add(place);

        pane.Add(new Label("Objects land at the Scene view pivot (snapped to terrain) and are fully configured: " +
                           "channel, event id, inventory, persistent id."));

        RefreshPlacementInputs();
        return pane;
    }

    private ObjectField MakeObjectField<T>(string label, VisualElement parent, out VisualElement row) where T : UnityEngine.Object
    {
        var field = new ObjectField(label) { objectType = typeof(T), allowSceneObjects = false };
        parent.Add(field);
        row = field;
        return field;
    }

    private PlacementTemplateSO CurrentTemplate()
    {
        if (templateDropdown == null || settings.Templates == null) return null;

        int index = templateDropdown.index;
        return index >= 0 && index < settings.Templates.Length ? settings.Templates[index] : null;
    }

    private void RefreshPlacementInputs()
    {
        PlacementTemplateSO template = CurrentTemplate();
        if (template == null) return;

        SetVisible(prefabRow, template.Prefab == null);
        SetVisible(eventRow, template.Uses(PlacementValueSource.EventId));
        SetVisible(itemRow, template.Uses(PlacementValueSource.Item));
        SetVisible(itemCountRow, template.Uses(PlacementValueSource.ItemCount));
        SetVisible(lootRow, template.Uses(PlacementValueSource.LootTable));
        SetVisible(questRow, template.Uses(PlacementValueSource.Quest));
    }

    private static void SetVisible(VisualElement element, bool visible)
    {
        if (element != null)
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void PlaceFromPlacementPane()
    {
        PlacementTemplateSO template = CurrentTemplate();
        if (template == null) return;

        var objective = objectiveField.value as QuestObjectiveSO;
        string eventId = objective != null ? objective.EventId : eventIdField.value;

        var item = itemField.value as ItemDefinitionSO;
        var quest = questField.value as QuestDefinitionSO;

        string label = item != null ? item.DisplayName
            : objective != null ? objective.DisplayLabel
            : template.DisplayName;

        var context = new PlacementContext
        {
            Settings = settings,
            Name = label,
            PrefabOverride = prefabField.value as GameObject,
            EventId = eventId,
            Item = item,
            ItemCount = Mathf.Max(1, itemCountField.value),
            LootTable = lootField.value as LootTableSO,
            Quest = quest
        };

        PlaceMany(template, context);
    }

    // ── Enemies pane ──────────────────────────────────────────────────────────

    private VisualElement BuildEnemiesPane()
    {
        enemiesPane = new AssetPane<EnemyDefinitionSO>(
            def => string.IsNullOrEmpty(def.DisplayName) ? def.name : def.DisplayName,
            new List<KeyValuePair<string, Action>> { new KeyValuePair<string, Action>("New Enemy", NewEnemyFlow) },
            new List<KeyValuePair<string, Action<EnemyDefinitionSO>>>
            {
                new KeyValuePair<string, Action<EnemyDefinitionSO>>("Place in Scene", PlaceEnemy)
            });

        return enemiesPane.Root;
    }

    private void NewEnemyFlow()
    {
        EditorInputPrompt.Show("New Enemy", "Enemy ID (e.g. ash_wraith)", "", enemyId =>
        {
            if (string.IsNullOrEmpty(enemyId)) return;

            EditorInputPrompt.Show("New Enemy", "Display name", "", displayName =>
            {
                if (string.IsNullOrEmpty(displayName)) return;

                // Every enemy gets its own loot table, so drops are authored per enemy from the start.
                var loot = EditorAssetUtility.CreateAsset(CreateInstance<LootTableSO>(), settings.LootRootFolder, enemyId + "_Loot");

                var enemy = CreateInstance<EnemyDefinitionSO>();
                enemy.EnemyId = enemyId;
                enemy.DisplayName = displayName;
                enemy.LootTable = loot;
                EditorAssetUtility.CreateAsset(enemy, settings.EnemyRootFolder, enemyId);

                enemiesPane.Reload(enemy);
                lootPane?.Reload();
            });
        });
    }

    private void PlaceEnemy(EnemyDefinitionSO enemy)
    {
        if (settings.EnemyTemplate == null)
        {
            Debug.LogWarning("[WorldAuthoringWindow] No EnemyTemplate in settings. Press \"Create Default Prefabs and Templates\".", settings);
            return;
        }

        if (enemy.Prefab == null)
        {
            Debug.LogWarning($"[WorldAuthoringWindow] '{enemy.name}' has no Prefab assigned.", enemy);
            return;
        }

        PlaceMany(settings.EnemyTemplate, new PlacementContext
        {
            Settings = settings,
            Name = string.IsNullOrEmpty(enemy.DisplayName) ? enemy.name : enemy.DisplayName,
            PrefabOverride = enemy.Prefab,
            LootTable = enemy.LootTable,
            EventId = enemy.DeathEventId
        });
    }

    // ── Items pane ────────────────────────────────────────────────────────────

    private VisualElement BuildItemsPane()
    {
        itemsPane = new AssetPane<ItemDefinitionSO>(
            item => string.IsNullOrEmpty(item.DisplayName) ? item.name : item.DisplayName,
            new List<KeyValuePair<string, Action>>
            {
                new KeyValuePair<string, Action>("New Misc Item", () => NewItemFlow<MiscItemSO>("Misc Item")),
                new KeyValuePair<string, Action>("New Armor", () => NewItemFlow<ArmorItemSO>("Armor")),
                new KeyValuePair<string, Action>("New Weapon", () => NewItemFlow<WeaponItemSO>("Weapon")),
                new KeyValuePair<string, Action>("Sync Database", SyncDatabase)
            },
            new List<KeyValuePair<string, Action<ItemDefinitionSO>>>
            {
                new KeyValuePair<string, Action<ItemDefinitionSO>>("Place as World Pickup", PlaceItemPickup)
            });

        return itemsPane.Root;
    }

    private void NewItemFlow<T>(string kind) where T : ItemDefinitionSO
    {
        EditorInputPrompt.Show($"New {kind}", "Display name", "", displayName =>
        {
            if (string.IsNullOrEmpty(displayName)) return;

            var item = CreateInstance<T>();
            item.DisplayName = displayName;
            item.ItemId = Guid.NewGuid().ToString("N");
            EditorAssetUtility.CreateAsset(item, settings.ItemRootFolder, displayName);

            RegisterInDatabase(item);
            itemsPane.Reload(item);
        });
    }

    private void RegisterInDatabase(ItemDefinitionSO item)
    {
        if (settings.ItemDatabase == null)
        {
            Debug.LogWarning("[WorldAuthoringWindow] No ItemDatabase in settings — the new item will not survive a save/load until it is added.", settings);
            return;
        }

        var database = new SerializedObject(settings.ItemDatabase);
        SerializedProperty items = database.FindProperty("items");
        items.InsertArrayElementAtIndex(items.arraySize);
        items.GetArrayElementAtIndex(items.arraySize - 1).objectReferenceValue = item;
        database.ApplyModifiedProperties();
    }

    /// <summary>Rebuilds the database from every item asset in the project.</summary>
    private void SyncDatabase()
    {
        if (settings.ItemDatabase == null)
        {
            Debug.LogWarning("[WorldAuthoringWindow] No ItemDatabase in settings.", settings);
            return;
        }

        List<ItemDefinitionSO> all = EditorAssetUtility.FindAssets<ItemDefinitionSO>();

        var database = new SerializedObject(settings.ItemDatabase);
        SerializedProperty items = database.FindProperty("items");
        items.arraySize = all.Count;

        for (int i = 0; i < all.Count; i++)
            items.GetArrayElementAtIndex(i).objectReferenceValue = all[i];

        database.ApplyModifiedProperties();
        Debug.Log($"[WorldAuthoringWindow] Item database now lists {all.Count} items.", settings.ItemDatabase);
    }

    private void PlaceItemPickup(ItemDefinitionSO item)
    {
        if (settings.ItemPickupTemplate == null)
        {
            Debug.LogWarning("[WorldAuthoringWindow] No ItemPickupTemplate in settings. Press \"Create Default Prefabs and Templates\".", settings);
            return;
        }

        PlaceMany(settings.ItemPickupTemplate, new PlacementContext
        {
            Settings = settings,
            Name = string.IsNullOrEmpty(item.DisplayName) ? item.name : item.DisplayName,
            Item = item,
            ItemCount = 1
        });
    }

    // ── Loot tables pane ──────────────────────────────────────────────────────

    private VisualElement BuildLootPane()
    {
        lootPane = new AssetPane<LootTableSO>(
            table => table.name,
            new List<KeyValuePair<string, Action>> { new KeyValuePair<string, Action>("New Loot Table", NewLootFlow) },
            new List<KeyValuePair<string, Action<LootTableSO>>>());

        return lootPane.Root;
    }

    private void NewLootFlow()
    {
        EditorInputPrompt.Show("New Loot Table", "Name (e.g. Wraith_Loot)", "", tableName =>
        {
            if (string.IsNullOrEmpty(tableName)) return;

            var table = EditorAssetUtility.CreateAsset(CreateInstance<LootTableSO>(), settings.LootRootFolder, tableName);
            lootPane.Reload(table);
        });
    }

    // ── Shared placement ──────────────────────────────────────────────────────

    private void PlaceMany(PlacementTemplateSO template, PlacementContext context)
    {
        int count = Mathf.Clamp(countField != null ? countField.value : 1, 1, 100);
        Vector3 origin = GetSpawnOrigin();

        GameObject last = null;
        for (int i = 0; i < count; i++)
        {
            Vector3 position = SnapToTerrain(origin + ScatterOffset(i, count));
            GameObject placed = PlacementWiring.Place(template, context, position);
            if (placed != null) last = placed;
        }

        if (last != null)
        {
            Selection.activeGameObject = last;
            EditorGUIUtility.PingObject(last);
        }
    }

    private static Vector3 GetSpawnOrigin()
    {
        SceneView view = SceneView.lastActiveSceneView;
        return view != null ? view.pivot : Vector3.zero;
    }

    private static Vector3 SnapToTerrain(Vector3 position)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) return position;

        position.y = terrain.SampleHeight(position) + terrain.transform.position.y;
        return position;
    }

    /// <summary>Spreads multiple placements on a ring so they do not stack on one point.</summary>
    private static Vector3 ScatterOffset(int index, int count)
    {
        if (count <= 1) return Vector3.zero;

        float angle = index * Mathf.PI * 2f / count;
        float radius = 1.5f * Mathf.Sqrt(count);
        return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
    }

    // ── Reusable list + inspector pane ────────────────────────────────────────

    /// <summary>
    /// Left: every asset of type T. Right: the selected asset's inspector plus
    /// action buttons. Top: creation buttons. One implementation serves the
    /// Enemies, Items, and Loot Tables tabs.
    /// </summary>
    private class AssetPane<T> where T : ScriptableObject
    {
        public readonly VisualElement Root = new VisualElement();

        private readonly ListView list;
        private readonly ScrollView detail = new ScrollView();
        private readonly List<T> assets = new List<T>();
        private readonly Func<T, string> labelOf;
        private readonly List<KeyValuePair<string, Action<T>>> selectedActions;
        private T selected;

        public AssetPane(Func<T, string> labelOf, List<KeyValuePair<string, Action>> creationActions,
            List<KeyValuePair<string, Action<T>>> selectedActions)
        {
            this.labelOf = labelOf;
            this.selectedActions = selectedActions;

            Root.style.flexGrow = 1;

            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.marginBottom = 4;
            foreach (KeyValuePair<string, Action> action in creationActions)
                toolbar.Add(new Button(action.Value) { text = action.Key });
            Root.Add(toolbar);

            var body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1;
            Root.Add(body);

            list = new ListView
            {
                makeItem = () => new Label(),
                bindItem = (element, index) => ((Label)element).text = labelOf(assets[index])
            };
            list.style.width = 220;
            list.itemsSource = assets;
            list.selectionChanged += selection =>
            {
                selected = selection.FirstOrDefault() as T;
                RefreshDetail();
            };
            body.Add(list);

            detail.style.flexGrow = 1;
            body.Add(detail);

            Reload();
        }

        public void Reload(T select = null)
        {
            assets.Clear();
            assets.AddRange(EditorAssetUtility.FindAssets<T>());
            list.Rebuild();

            if (select != null)
            {
                int index = assets.IndexOf(select);
                if (index >= 0) list.SetSelection(index);
            }
        }

        private void RefreshDetail()
        {
            detail.Clear();
            if (selected == null) return;

            detail.Add(new InspectorElement(selected));

            foreach (KeyValuePair<string, Action<T>> action in selectedActions)
            {
                T target = selected;
                Action<T> callback = action.Value;

                var button = new Button(() => callback(target)) { text = action.Key };
                button.style.marginTop = 6;
                button.style.height = 28;
                detail.Add(button);
            }
        }
    }
}