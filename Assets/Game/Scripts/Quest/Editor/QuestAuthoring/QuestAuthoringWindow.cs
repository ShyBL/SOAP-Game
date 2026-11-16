using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SOAP
{
    /// <summary>
    /// Quest authoring window: Quests (macro) | Objectives (hierarchy) |
    /// Properties (inspector for whichever is selected deepest) across the
    /// top, plus an NPC Setup pane along the bottom that reacts to whichever
    /// quest is selected in Pane A.
    ///
    /// Folder and naming are driven by a QuestAuthoringSettings asset
    /// (found via AssetDatabase, auto-created with sensible defaults if none
    /// exists) rather than hardcoded — see QuestAuthoringSettings.cs.
    /// NpcAuthoringSettings is loaded the same way for the NPC pane — kept
    /// as a separate asset rather than merged into QuestAuthoringSettings,
    /// since it's a genuinely separate concern (registry, spawn prefab)
    /// authored by a different part of the pipeline.
    ///
    /// Pane C is built from InspectorElement bound to a live SerializedObject
    /// rather than hand-written fields — lets CountUpObjectiveSO and
    /// BoolFlagObjectiveSO each show exactly their own fields with zero
    /// branching code here, and lets [SOCreate]'s inline Create button work
    /// inside this window unmodified.
    ///
    /// Pane B (Objectives) supports drag-reordering, synced back into the
    /// real Objectives array. Pane A (Quests) deliberately does not — no
    /// backing store for a custom order to persist into.
    ///
    /// The NPC pane (bottom) absorbs what used to be the standalone
    /// NpcAuthoringWindow, now deleted — see the "Pane D: NPC Setup" region
    /// below. Unlike that window's flat NPC-registry browser, this pane has
    /// no list of its own; it's purely a function of Pane A's selection:
    /// no quest selected, quest selected with no NPC offering it yet, or
    /// quest selected with an NPC already offering it (mini-inspector +
    /// Spawn button).
    /// </summary>
    public class QuestAuthoringWindow : EditorWindow
    {
        
        private const string UxmlPath = "Assets/Game/Scripts/Quest/Editor/QuestAuthoring/QuestAuthoringWindow.uxml";
        private const string UssPath  = "Assets/Game/Scripts/Quest/Editor/QuestAuthoring/QuestAuthoringWindow.uss";

        private ListView   questListView;
        private ListView   objectiveListView;
        private ScrollView inspectorScrollView;
        private Label      propertiesHeaderLabel;
        private ScrollView npcPaneContent;

        private List<QuestDefinitionSO> allQuests         = new List<QuestDefinitionSO>();
        private List<QuestDefinitionSO> displayedQuests   = new List<QuestDefinitionSO>();
        private List<QuestObjectiveSO>  currentObjectives = new List<QuestObjectiveSO>();

        private string currentSearchText = string.Empty;

        private QuestDefinitionSO selectedQuest;
        private QuestObjectiveSO  selectedObjective;

        private ToolbarSearchField searchField;

        // Cached once per window session — re-fetched lazily if null (e.g.
        // after a domain reload), not re-scanned on every single action.
        private QuestAuthoringSettings settings;

        // Same lazy-cache pattern as `settings`, for the NPC pane absorbed
        // from the now-deleted NpcAuthoringWindow. Kept as a separate asset
        // rather than merged into QuestAuthoringSettings — see note at the
        // bottom of this file if that split ever needs revisiting.
        private NpcAuthoringSettings npcSettings;

        // The NpcQuestGiver whose Quests[] contains the currently selected
        // quest, if any — recomputed by RefreshNpcPane() every time Pane A's
        // selection changes. Null when no quest is selected, or none of the
        // registry's NPCs currently offer the selected quest.
        private NpcQuestGiver assignedNpcForSelectedQuest;

        [MenuItem("SOAP/Quest Authoring")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<QuestAuthoringWindow>();
            wnd.titleContent = new GUIContent("Quest Authoring");
        }

        public void CreateGUI()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);

            if (visualTree == null)
            {
                rootVisualElement.Add(new Label(
                    $"[QuestAuthoringWindow] Could not load UXML at '{UxmlPath}'. " +
                    "Update the UxmlPath constant if the file was moved."));
                return;
            }

            VisualElement root = visualTree.Instantiate();
            root.style.flexGrow = 1;
            if (styleSheet != null) root.styleSheets.Add(styleSheet);
            rootVisualElement.Add(root);

            questListView         = root.Q<ListView>("quest-list-view");
            objectiveListView     = root.Q<ListView>("objective-list-view");
            inspectorScrollView   = root.Q<ScrollView>("inspector-scroll-view");
            propertiesHeaderLabel = root.Q<Label>("properties-header-label");
            npcPaneContent         = root.Q<ScrollView>("npc-pane-content");

            var addQuestButton        = root.Q<Button>("btn-add-quest");
            var removeQuestButton     = root.Q<Button>("btn-remove-quest");
            var addObjectiveButton    = root.Q<Button>("btn-add-objective");
            var removeObjectiveButton = root.Q<Button>("btn-remove-objective");
            var saveButton            = root.Q<ToolbarButton>("toolbar-btn-save");
            var settingsButton        = root.Q<ToolbarButton>("toolbar-btn-settings");
            searchField                = root.Q<ToolbarSearchField>("toolbar-search");

            addQuestButton.clicked        += AddQuest;
            removeQuestButton.clicked     += RemoveSelectedQuest;
            addObjectiveButton.clicked    += AddObjective;
            removeObjectiveButton.clicked += RemoveSelectedObjective;
            saveButton.clicked            += () => AssetDatabase.SaveAssets();
            if (settingsButton != null) settingsButton.clicked += OpenSettings;
            searchField.RegisterValueChangedCallback(evt => ApplyQuestFilter(evt.newValue));

            SetupQuestList();
            SetupObjectiveList();

            RefreshQuestList();
            RefreshNpcPane();
        }

        // ── Settings ──────────────────────────────────────────────────────────

        private void OpenSettings()
        {
            // Opens the unified Project Settings tab directly
            SettingsService.OpenProjectSettings("Project/SOAP Authoring");
        }
        
        private QuestAuthoringSettings GetSettings()
        {
            if (settings != null) return settings;

            var guids = AssetDatabase.FindAssets("t:QuestAuthoringSettings");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                settings = AssetDatabase.LoadAssetAtPath<QuestAuthoringSettings>(path);
            }
    
            if (settings == null)
                Debug.LogWarning("[QuestAuthoringWindow] Quest settings not found. Click '⚙ Settings' to initialize.");
        
            return settings;
        }

        private NpcAuthoringSettings GetNpcSettings()
        {
            if (npcSettings != null) return npcSettings;

            var guids = AssetDatabase.FindAssets("t:NpcAuthoringSettings");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                npcSettings = AssetDatabase.LoadAssetAtPath<NpcAuthoringSettings>(path);
            }
    
            if (npcSettings == null)
                Debug.LogWarning("[QuestAuthoringWindow] NPC settings not found. Click '⚙ Settings' to initialize.");
        
            return npcSettings;
        }
        
        // private void OpenSettings()
        // {
        //     var s = GetSettings();
        //     Selection.activeObject = s;
        //     EditorGUIUtility.PingObject(s);
        // }
        
        // private QuestAuthoringSettings GetSettings()
        // {
        //     if (settings != null) return settings;
        //
        //     var guids = AssetDatabase.FindAssets("t:QuestAuthoringSettings");
        //     if (guids.Length > 0)
        //     {
        //         var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        //         settings = AssetDatabase.LoadAssetAtPath<QuestAuthoringSettings>(path);
        //         if (settings != null) return settings;
        //     }
        //
        //     // None found — create one with defaults so the window works out
        //     // of the box without a manual setup step.
        //     EnsureFolder("Assets/Editor");
        //     var instance = ScriptableObject.CreateInstance<QuestAuthoringSettings>();
        //     AssetDatabase.CreateAsset(instance, "Assets/Editor/QuestAuthoringSettings.asset");
        //     AssetDatabase.SaveAssets();
        //     AssetDatabase.Refresh();
        //
        //     settings = instance;
        //     return settings;
        // }
        //
        // /// <summary>
        // /// Same lazy-find-or-create pattern as GetSettings(), for the
        // /// NpcAuthoringSettings absorbed from the deleted NpcAuthoringWindow.
        // /// Note the created default asset will still need Registry and
        // /// MasterNpcPrefab assigned by hand — those can't be sensibly
        // /// auto-populated, unlike the folder/template string defaults.
        // /// </summary>
        // private NpcAuthoringSettings GetNpcSettings()
        // {
        //     if (npcSettings != null) return npcSettings;
        //
        //     var guids = AssetDatabase.FindAssets("t:NpcAuthoringSettings");
        //     if (guids.Length > 0)
        //     {
        //         var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        //         npcSettings = AssetDatabase.LoadAssetAtPath<NpcAuthoringSettings>(path);
        //         if (npcSettings != null) return npcSettings;
        //     }
        //
        //     EnsureFolder("Assets/Editor");
        //     var instance = ScriptableObject.CreateInstance<NpcAuthoringSettings>();
        //     AssetDatabase.CreateAsset(instance, "Assets/Editor/NpcAuthoringSettings.asset");
        //     AssetDatabase.SaveAssets();
        //     AssetDatabase.Refresh();
        //
        //     npcSettings = instance;
        //     return npcSettings;
        // }

        // ── Pane A: Quests ────────────────────────────────────────────────────

        private void SetupQuestList()
        {
            questListView.makeItem = () => new Label();
            questListView.bindItem = (element, i) =>
            {
                var quest = displayedQuests[i];
                ((Label)element).text = quest == null
                    ? "(missing)"
                    : string.IsNullOrEmpty(quest.DisplayName) ? quest.name : quest.DisplayName;
            };
            questListView.itemsSource = displayedQuests;
            questListView.selectionChanged += OnQuestSelectionChanged;
        }

        private void RefreshQuestList()
        {
            allQuests = FindAllAssetsOfType<QuestDefinitionSO>()
                .OrderBy(q => string.IsNullOrEmpty(q.DisplayName) ? q.name : q.DisplayName)
                .ToList();

            ApplyQuestFilter(currentSearchText);
        }

        private void ApplyQuestFilter(string searchText)
        {
            currentSearchText = searchText ?? string.Empty;

            displayedQuests = string.IsNullOrEmpty(currentSearchText)
                ? new List<QuestDefinitionSO>(allQuests)
                : allQuests.Where(q => MatchesSearch(q, currentSearchText)).ToList();

            questListView.itemsSource = displayedQuests;
            questListView.Rebuild();
        }

        private static bool MatchesSearch(QuestDefinitionSO quest, string searchText)
        {
            if (quest == null) return false;

            bool Contains(string field) =>
                !string.IsNullOrEmpty(field) &&
                field.IndexOf(searchText, System.StringComparison.OrdinalIgnoreCase) >= 0;

            return Contains(quest.DisplayName) || Contains(quest.QuestId) || Contains(quest.name);
        }

        private void OnQuestSelectionChanged(IEnumerable<object> selection)
        {
            selectedQuest = selection.FirstOrDefault() as QuestDefinitionSO;

            // Critical: without this, selectedObjective can keep pointing at
            // a different quest's objective after switching quests here without
            // touching Pane B first — clicking Remove would then delete that
            // stale objective's asset while searching the WRONG quest's array
            // for it, silently corrupting the previous quest's Objectives list.
            selectedObjective = null;
            objectiveListView.ClearSelection();

            RefreshObjectiveList();
            ShowInspectorFor(selectedQuest, "Quest Properties");
            RefreshNpcPane();
        }

        // ── Pane B: Objectives ────────────────────────────────────────────────

        private void SetupObjectiveList()
        {
            objectiveListView.makeItem = () => new Label();
            objectiveListView.bindItem = (element, i) =>
            {
                var objective = currentObjectives[i];
                ((Label)element).text = objective == null
                    ? "(missing)"
                    : string.IsNullOrEmpty(objective.DisplayLabel) ? objective.name : objective.DisplayLabel;
            };
            objectiveListView.itemsSource = currentObjectives;
            objectiveListView.selectionChanged += OnObjectiveSelectionChanged;

            objectiveListView.reorderable = true;
            objectiveListView.reorderMode = ListViewReorderMode.Animated;
            objectiveListView.itemIndexChanged += OnObjectiveReordered;
        }

        private void RefreshObjectiveList()
        {
            currentObjectives = selectedQuest?.Objectives != null
                ? selectedQuest.Objectives.ToList()
                : new List<QuestObjectiveSO>();

            objectiveListView.itemsSource = currentObjectives;
            objectiveListView.Rebuild();
        }

        private void OnObjectiveReordered(int oldIndex, int newIndex)
        {
            if (selectedQuest == null) return;

            var serializedQuest = new SerializedObject(selectedQuest);
            var objectivesProp  = serializedQuest.FindProperty(nameof(QuestDefinitionSO.Objectives));

            objectivesProp.MoveArrayElement(oldIndex, newIndex);
            serializedQuest.ApplyModifiedProperties();
        }

        private void OnObjectiveSelectionChanged(IEnumerable<object> selection)
        {
            selectedObjective = selection.FirstOrDefault() as QuestObjectiveSO;

            if (selectedObjective != null)
                ShowInspectorFor(selectedObjective, "Objective Properties");
            else
                ShowInspectorFor(selectedQuest, "Quest Properties");
        }

        // ── Pane C: Properties ───────────────────────────────────────────────

        private void ShowInspectorFor(Object target, string headerText)
        {
            inspectorScrollView.Clear();
            propertiesHeaderLabel.text = headerText;

            if (target == null) return;

            var serializedObject = new SerializedObject(target);
            var inspector = new InspectorElement(serializedObject);
            inspectorScrollView.Add(inspector);
        }

        // ── Add / Remove: Quests ─────────────────────────────────────────────

        private void AddQuest()
        {
            EditorInputPrompt.Show("New Quest", "Quest ID", "", questId =>
            {
                if (string.IsNullOrEmpty(questId)) return;

                var s          = GetSettings();
                var questFolder = $"{s.QuestRootFolder}/{questId}";
                EnsureFolder(questFolder);

                var fileName = QuestAuthoringSettings.ApplyTemplate(
                    s.QuestDefinitionNameTemplate, ("QuestId", questId));

                var instance = ScriptableObjectSubtypeMenu.CreateAsset(typeof(QuestDefinitionSO), questFolder, fileName);
                var quest    = instance as QuestDefinitionSO;
                if (quest != null) quest.QuestId = questId;

                currentSearchText = string.Empty;
                searchField?.SetValueWithoutNotify(string.Empty);
                RefreshQuestList();

                int index = displayedQuests.IndexOf(quest);
                if (index >= 0)
                    questListView.SetSelection(index);
            });
        }

        private void RemoveSelectedQuest()
        {
            if (selectedQuest == null)
            {
                Debug.LogWarning("[QuestAuthoringWindow] No quest selected to remove.");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Delete Quest?",
                $"Delete '{selectedQuest.DisplayName}' ({selectedQuest.QuestId})? " +
                "This deletes the quest AND every one of its objective assets — cannot be undone.",
                "Delete", "Cancel");

            if (!confirmed) return;

            // A quest being deleted may still be referenced from one or more
            // NpcQuestGiver.Quests[] arrays (assigned via the NPC pane below).
            // Strip those references before the quest asset itself is gone —
            // otherwise every such NPC is left holding a dangling reference to
            // a deleted asset, the same class of orphan-reference bug the
            // objective cleanup below already guards against.
            RemoveQuestFromAllNpcs(GetNpcSettings().Registry, selectedQuest);

            // Objectives are separate, standalone assets — deleting only the
            // quest itself would leave them orphaned on disk with nothing
            // referencing them. Clean them up first.
            if (selectedQuest.Objectives != null)
            {
                foreach (var objective in selectedQuest.Objectives)
                {
                    if (objective == null) continue;
                    var objectivePath = AssetDatabase.GetAssetPath(objective);
                    if (!string.IsNullOrEmpty(objectivePath))
                        AssetDatabase.DeleteAsset(objectivePath);
                }
            }

            var path = AssetDatabase.GetAssetPath(selectedQuest);
            AssetDatabase.DeleteAsset(path);

            selectedQuest = null;
            selectedObjective = null;
            objectiveListView.ClearSelection();

            RefreshQuestList();
            RefreshObjectiveList();
            ShowInspectorFor(null, "Properties");
            RefreshNpcPane();
        }

        // ── Add / Remove: Objectives ─────────────────────────────────────────

        private void AddObjective()
        {
            if (selectedQuest == null)
            {
                Debug.LogWarning("[QuestAuthoringWindow] Select a quest before adding an objective.");
                return;
            }

            // Captured now, at the moment of the click — not re-read from the
            // mutable selectedQuest field later. Both the subtype menu and the
            // EditorInputPrompt that follows are non-modal; without capturing
            // this, switching Pane A's selection while either is still open
            // would attach the new objective to whichever quest happens to be
            // selected when the prompt is confirmed, not the one that was
            // selected when "+ Add Objective" was actually clicked.
            var targetQuest = selectedQuest;

            ScriptableObjectSubtypeMenu.PickType(typeof(QuestObjectiveSO),
                concreteType => OnObjectiveTypePicked(concreteType, targetQuest));
        }

        private void OnObjectiveTypePicked(System.Type concreteType, QuestDefinitionSO targetQuest)
        {
            EditorInputPrompt.Show("New Objective", "Objective Name", "", shortName =>
            {
                if (string.IsNullOrEmpty(shortName) || targetQuest == null) return;

                var s      = GetSettings();
                var folder = GetFolderFor(targetQuest);

                var fileName = QuestAuthoringSettings.ApplyTemplate(
                    s.ObjectiveNameTemplate,
                    ("QuestId", targetQuest.QuestId), ("ShortName", shortName));

                var instance  = ScriptableObjectSubtypeMenu.CreateAsset(concreteType, folder, fileName);
                var objective = instance as QuestObjectiveSO;
                if (objective != null)
                {
                    objective.DisplayLabel = ObjectNames.NicifyVariableName(shortName);
                    objective.QuestId      = targetQuest.QuestId;
                }


                OnObjectiveCreated(instance, targetQuest);
            });
        }

        private void OnObjectiveCreated(ScriptableObject instance, QuestDefinitionSO targetQuest)
        {
            if (!(instance is QuestObjectiveSO objective) || targetQuest == null) return;

            var serializedQuest = new SerializedObject(targetQuest);
            var objectivesProp  = serializedQuest.FindProperty(nameof(QuestDefinitionSO.Objectives));

            int index = objectivesProp.arraySize;
            objectivesProp.InsertArrayElementAtIndex(index);
            objectivesProp.GetArrayElementAtIndex(index).objectReferenceValue = objective;

            serializedQuest.ApplyModifiedProperties();

            // Only refresh/select if the target is still the quest actually
            // selected in Pane A right now — if the user switched away while
            // the prompt was open, the new objective was correctly attached to
            // targetQuest above, but Pane B shouldn't jump to show a different
            // quest's list than whatever's currently selected.
            if (targetQuest == selectedQuest)
            {
                RefreshObjectiveList();

                int newIndex = currentObjectives.IndexOf(objective);
                if (newIndex >= 0)
                    objectiveListView.SetSelection(newIndex);
            }
        }

        private void RemoveSelectedObjective()
        {
            if (selectedObjective == null || selectedQuest == null)
            {
                Debug.LogWarning("[QuestAuthoringWindow] No objective selected to remove.");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Delete Objective?",
                $"Delete '{selectedObjective.DisplayLabel}'? " +
                "This removes it from the quest and deletes the asset file — cannot be undone.",
                "Delete", "Cancel");

            if (!confirmed) return;

            var serializedQuest = new SerializedObject(selectedQuest);
            var objectivesProp  = serializedQuest.FindProperty(nameof(QuestDefinitionSO.Objectives));

            for (int i = 0; i < objectivesProp.arraySize; i++)
            {
                var element = objectivesProp.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == selectedObjective)
                {
                    // Unity's well-documented Object-reference array footgun:
                    // DeleteArrayElementAtIndex on a slot that still holds a
                    // non-null reference only NULLS that slot — it doesn't
                    // remove it. The reference has to be cleared first, then
                    // deleted, or the array grows a null gap instead of
                    // actually shrinking.
                    element.objectReferenceValue = null;
                    objectivesProp.DeleteArrayElementAtIndex(i);
                    break;
                }
            }

            serializedQuest.ApplyModifiedProperties();

            var path = AssetDatabase.GetAssetPath(selectedObjective);
            AssetDatabase.DeleteAsset(path);

            selectedObjective = null;
            objectiveListView.ClearSelection();

            RefreshObjectiveList();
            ShowInspectorFor(selectedQuest, "Quest Properties");
        }

        // ── Pane D: NPC Setup ────────────────────────────────────────────────
        //
        // Absorbed from the now-deleted NpcAuthoringWindow. Unlike that
        // window's flat NPC registry browser, this pane is driven entirely by
        // Pane A's current selection — it answers one question, "which NPC
        // (if any) offers the selected quest", and offers the matching
        // action: create one, assign an existing one, or spawn the one
        // that's already assigned.

        private void RefreshNpcPane()
        {
            var npcS = GetNpcSettings();
            if (npcS == null)
            {
                npcPaneContent.Add(new Label("Please initialize NPC Settings via the ⚙ Settings menu."));
                return;
            }
            
            npcPaneContent.Clear();

            if (selectedQuest == null)
            {
                assignedNpcForSelectedQuest = null;
                var placeholder = new Label("Select a Quest above to configure its NPC.");
                placeholder.AddToClassList("npc-pane-placeholder");
                npcPaneContent.Add(placeholder);
                return;
            }

            //var npcS = GetNpcSettings();
            assignedNpcForSelectedQuest = FindNpcForQuest(npcS.Registry, selectedQuest);

            if (assignedNpcForSelectedQuest == null)
                BuildUnassignedNpcPane(npcS);
            else
                BuildAssignedNpcPane(assignedNpcForSelectedQuest, npcS);
        }

        private static NpcQuestGiver FindNpcForQuest(QuestGiverRegistry registry, QuestDefinitionSO quest)
        {
            if (registry?.NpcGivers == null) return null;

            foreach (var giver in registry.NpcGivers)
            {
                if (giver != null && giver.Quests != null && giver.Quests.Contains(quest))
                    return giver;
            }

            return null;
        }

        // State 2: quest selected, nothing offers it yet.
        private void BuildUnassignedNpcPane(NpcAuthoringSettings npcS)
        {
            npcPaneContent.Add(new Label("No NPC is currently assigned to offer this quest."));

            var createButton = new Button(() => CreateAndAssignNpcFlow(npcS))
            {
                text = "+ Create & Assign NPC"
            };
            createButton.AddToClassList("pane-footer-btn");
            npcPaneContent.Add(createButton);

            var existing = npcS.Registry?.NpcGivers?.Where(g => g != null).ToList();
            if (existing == null || existing.Count == 0) return;

            npcPaneContent.Add(new VisualElement { style = { height = 8 } });

            var choices  = existing.Select(g => string.IsNullOrEmpty(g.DisplayName) ? g.NpcId : g.DisplayName).ToList();
            var dropdown = new DropdownField("Assign to existing NPC", choices, 0);
            npcPaneContent.Add(dropdown);

            var assignButton = new Button(() =>
            {
                if (dropdown.index < 0 || dropdown.index >= existing.Count) return;
                AssignQuestToExistingNpc(existing[dropdown.index]);
            })
            { text = "Assign" };
            assignButton.AddToClassList("pane-footer-btn");
            npcPaneContent.Add(assignButton);
        }

        // State 3: quest selected, an NPC already offers it.
        private void BuildAssignedNpcPane(NpcQuestGiver npc, NpcAuthoringSettings npcS)
        {
            var serializedNpc = new SerializedObject(npc);
            var inspector = new InspectorElement(serializedNpc);
            npcPaneContent.Add(inspector);

            npcPaneContent.Add(new VisualElement { style = { height = 12 } });

            var spawnButton = new Button(() => NpcSceneSetup.SpawnNpcInScene(npc, npcS))
            {
                text = "Spawn in Scene"
            };
            spawnButton.AddToClassList("pane-footer-btn");
            npcPaneContent.Add(spawnButton);
        }

        private void AssignQuestToExistingNpc(NpcQuestGiver npc)
        {
            if (npc == null || selectedQuest == null) return;

            var serializedNpc = new SerializedObject(npc);
            var questsProp    = serializedNpc.FindProperty(nameof(NpcQuestGiver.Quests));

            int index = questsProp.arraySize;
            questsProp.InsertArrayElementAtIndex(index);
            questsProp.GetArrayElementAtIndex(index).objectReferenceValue = selectedQuest;
            serializedNpc.ApplyModifiedProperties();

            RefreshNpcPane();
        }

        private void CreateAndAssignNpcFlow(NpcAuthoringSettings npcS)
        {
            // Same capture-at-click-time reasoning as AddObjective's
            // targetQuest — EditorInputPrompt is non-modal, so Pane A's
            // selection could change while either prompt is still open.
            var targetQuest = selectedQuest;

            EditorInputPrompt.Show("NPC Setup", "NPC ID (e.g. captain_aldric)", "", npcId =>
            {
                if (string.IsNullOrEmpty(npcId) || targetQuest == null) return;

                EditorInputPrompt.Show("NPC Setup", "Display Name", "", displayName =>
                {
                    if (string.IsNullOrEmpty(displayName)) return;

                    if (npcS.Registry == null)
                    {
                        Debug.LogError("[QuestAuthoringWindow] NpcAuthoringSettings has no Registry " +
                                       "assigned — cannot create/register an NPC. Assign one via " +
                                       "the NpcAuthoringSettings asset first.");
                        return;
                    }

                    EnsureFolder(npcS.NpcRootFolder);

                    var newNpc = ScriptableObject.CreateInstance<NpcQuestGiver>();
                    newNpc.NpcId = npcId;
                    newNpc.DisplayName = displayName;
                    newNpc.Quests = new[] { targetQuest };

                    var fileName = npcS.NpcQuestGiverNameTemplate.Replace("{NpcId}", npcId);
                    var path = AssetDatabase.GenerateUniqueAssetPath($"{npcS.NpcRootFolder}/{fileName}.asset");

                    AssetDatabase.CreateAsset(newNpc, path);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    var serializedRegistry = new SerializedObject(npcS.Registry);
                    var arrayProp = serializedRegistry.FindProperty(nameof(QuestGiverRegistry.NpcGivers));
                    int index = arrayProp.arraySize;
                    arrayProp.InsertArrayElementAtIndex(index);
                    arrayProp.GetArrayElementAtIndex(index).objectReferenceValue = newNpc;
                    serializedRegistry.ApplyModifiedProperties();

                    // Same "only refresh if still the current selection" guard
                    // as OnObjectiveCreated — the user may have switched Pane A's
                    // selection while the two prompts above were open.
                    if (targetQuest == selectedQuest)
                        RefreshNpcPane();
                });
            });
        }

        /// <summary>
        /// Strips any reference to `quest` out of every NpcQuestGiver's
        /// Quests[] array in the registry. Uses the same null-then-delete
        /// order as RemoveSelectedObjective — DeleteArrayElementAtIndex on a
        /// slot that still holds an Object reference only nulls that slot
        /// rather than removing it, so the reference must be cleared first.
        /// </summary>
        private static void RemoveQuestFromAllNpcs(QuestGiverRegistry registry, QuestDefinitionSO quest)
        {
            if (registry?.NpcGivers == null) return;

            foreach (var giver in registry.NpcGivers)
            {
                if (giver == null) continue;

                var serializedNpc = new SerializedObject(giver);
                var questsProp    = serializedNpc.FindProperty(nameof(NpcQuestGiver.Quests));

                for (int i = 0; i < questsProp.arraySize; i++)
                {
                    var element = questsProp.GetArrayElementAtIndex(i);
                    if (element.objectReferenceValue == quest)
                    {
                        element.objectReferenceValue = null;
                        questsProp.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }

                serializedNpc.ApplyModifiedProperties();
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static List<T> FindAllAssetsOfType<T>() where T : Object
        {
            var results = new List<T>();
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");

            foreach (var guid in guids)
            {
                var path  = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) results.Add(asset);
            }

            return results;
        }

        private static string GetFolderFor(Object owner)
        {
            var ownerPath = AssetDatabase.GetAssetPath(owner);
            return string.IsNullOrEmpty(ownerPath)
                ? "Assets"
                : Path.GetDirectoryName(ownerPath)?.Replace('\\', '/') ?? "Assets";
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;

            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            var name   = Path.GetFileName(folder);

            if (string.IsNullOrEmpty(parent)) return;
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}