using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using SOAP;

public class NpcAuthoringWindow : EditorWindow
{
    [SerializeField] private VisualTreeAsset windowLayout;
    
    private ListView npcListView;
    private ScrollView paneBContainer;
    private ScrollView paneCContainer;
    
    private NpcAuthoringSettings settings;
    private SerializedObject serializedRegistry;
    private NpcQuestGiver selectedNpc;

    [MenuItem("SOAP/NPC Authoring Window")]
    public static void ShowWindow()
    {
        var window = GetWindow<NpcAuthoringWindow>("NPC Authoring");
        window.minSize = new Vector2(800, 500);
    }

    private void CreateGUI()
    {
        if (windowLayout != null) windowLayout.CloneTree(rootVisualElement);
        
        LoadSettings();
        if (settings == null || settings.Registry == null)
        {
            rootVisualElement.Add(new Label("Please assign settings and Registry in NpcAuthoringSettings."));
            return;
        }

        serializedRegistry = new SerializedObject(settings.Registry);

        // Map UI Elements (Assuming your standard 3-pane UXML layout names)
        npcListView = rootVisualElement.Q<ListView>("pane-a-list");
        paneBContainer = rootVisualElement.Q<ScrollView>("pane-b-content");
        paneCContainer = rootVisualElement.Q<ScrollView>("pane-c-content");

        // Pane A Actions
        rootVisualElement.Q<Button>("btn-add-npc")?.RegisterCallback<ClickEvent>(evt => AddNpcFlow());
        rootVisualElement.Q<Button>("btn-remove-npc")?.RegisterCallback<ClickEvent>(evt => RemoveSelectedNpc());

        SetupNpcList();
    }

    private void LoadSettings()
    {
        var guids = AssetDatabase.FindAssets("t:NpcAuthoringSettings");
        if (guids.Length > 0)
            settings = AssetDatabase.LoadAssetAtPath<NpcAuthoringSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    private void SetupNpcList()
    {
        npcListView.makeItem = () => new Label();
        npcListView.bindItem = (element, i) =>
        {
            var label = (Label)element;
            var npc = settings.Registry.NpcGivers[i];
            label.text = npc != null ? npc.NpcId : "Null Element";
        };
        
        npcListView.itemsSource = settings.Registry.NpcGivers;
        npcListView.selectionChanged += selections => 
        {
            selectedNpc = selections.FirstOrDefault() as NpcQuestGiver;
            RefreshPaneB();
            RefreshPaneC();
        };
    }

    private void RefreshPaneB()
    {
        paneBContainer.Clear();
        if (selectedNpc == null) return;

        // Display the Quests array using Unity's native PropertyField for reordering and adding
        var serializedNpc = new SerializedObject(selectedNpc);
        var questsProp = serializedNpc.FindProperty("Quests");
        
        var questsField = new PropertyField(questsProp);
        questsField.Bind(serializedNpc);
        paneBContainer.Add(questsField);

        // Add Spacer
        paneBContainer.Add(new VisualElement { style = { height = 20 } });

        // Spawn Button
        var spawnBtn = new Button(() => NpcSceneSetup.SpawnNpcInScene(selectedNpc, settings))
        {
            text = "Spawn in Scene",
            style = { height = 40, backgroundColor = new Color(0.2f, 0.4f, 0.2f) }
        };
        paneBContainer.Add(spawnBtn);
    }

    private void RefreshPaneC()
    {
        paneCContainer.Clear();
        if (selectedNpc == null) return;

        var inspector = new InspectorElement(selectedNpc);
        paneCContainer.Add(inspector);
    }

    private void AddNpcFlow()
    {
        EditorInputPrompt.Show("NPC Setup", "NPC ID (e.g. captain_aldric)", "", npcId =>
        {
            if (string.IsNullOrEmpty(npcId)) return;
            
            EditorInputPrompt.Show("NPC Setup", "Display Name", "", displayName =>
            {
                if (string.IsNullOrEmpty(displayName)) return;

                // 1. Ensure the target folder exists within Unity's AssetDatabase
                EnsureFolder(settings.NpcRootFolder);

                // 2. Create SO instance
                var newNpc = CreateInstance<NpcQuestGiver>();
                newNpc.NpcId = npcId;
                newNpc.DisplayName = displayName;

                // 3. Save Asset safely
                string safeName = settings.NpcQuestGiverNameTemplate.Replace("{NpcId}", npcId);
                string path = $"{settings.NpcRootFolder}/{safeName}.asset";
                path = AssetDatabase.GenerateUniqueAssetPath(path);
                
                AssetDatabase.CreateAsset(newNpc, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // 4. Add to Registry
                serializedRegistry.Update();
                var arrayProp = serializedRegistry.FindProperty("NpcGivers");
                arrayProp.InsertArrayElementAtIndex(arrayProp.arraySize);
                arrayProp.GetArrayElementAtIndex(arrayProp.arraySize - 1).objectReferenceValue = newNpc;
                serializedRegistry.ApplyModifiedProperties();

                // 5. Update UI source and selection
                npcListView.itemsSource = settings.Registry.NpcGivers;
                npcListView.RefreshItems();
                npcListView.SetSelection(settings.Registry.NpcGivers.Length - 1);
            });
        });
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        var parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
        var name   = System.IO.Path.GetFileName(folder);

        if (string.IsNullOrEmpty(parent)) return;
        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, name);
    }

    private void RemoveSelectedNpc()
    {
        if (selectedNpc == null) return;
        
        serializedRegistry.Update();
        var arrayProp = serializedRegistry.FindProperty("NpcGivers");
        
        for (int i = 0; i < arrayProp.arraySize; i++)
        {
            if (arrayProp.GetArrayElementAtIndex(i).objectReferenceValue == selectedNpc)
            {
                arrayProp.DeleteArrayElementAtIndex(i);
                // Delete again to clear null reference left by Unity's Array delete bug
                if (arrayProp.arraySize > i && arrayProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    arrayProp.DeleteArrayElementAtIndex(i);
                break;
            }
        }
        
        serializedRegistry.ApplyModifiedProperties();
        
        // Optionally delete the asset here via AssetDatabase.DeleteAsset() if desired

        selectedNpc = null;
        npcListView.RefreshItems();
        RefreshPaneB();
        RefreshPaneC();
    }
}