using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmpireAtWar.Editor.Balance
{
    public sealed class BalanceEditorWindow : EditorWindow
    {
        private const string MENU_PATH = "Tools/Empire At War/Game Design/Balance Editor";
        private const string RECOVERY_PATH = "Library/BalanceEditor/draft.json";
        private const string UI_PATH = "Assets/Scripts/Editor/Balance/BalanceEditor";

        [SerializeField] private BalanceWindowState state = new BalanceWindowState();
        [SerializeField] private BalancePreset preset;
        [SerializeField] private BalancePresetScope presetScope = BalancePresetScope.FullRegisteredSet;

        private BalanceRegistration _registry;
        private BalanceEditorController _controller;
        private string _message = "";
        private VisualElement _details;
        private bool _refreshPending;

        [MenuItem(MENU_PATH)]
        public static void Open() => EditorHubWindow.OpenTool(MENU_PATH);

        private void OnEnable()
        {
            if (state.Draft.Changes.Count == 0 && File.Exists(RECOVERY_PATH))
                state = JsonUtility.FromJson<BalanceWindowState>(File.ReadAllText(RECOVERY_PATH));
            _controller = new BalanceEditorController(state, Refresh);
            saveChangesMessage = "Save the staged balance draft as a preset before closing? Gameplay assets are written only through Review/Apply.";
            EditorApplication.playModeStateChanged += PlayModeChanged;
        }

        private void OnDisable()
        {
            RememberScroll();
            Persist();
            EditorApplication.playModeStateChanged -= PlayModeChanged;
        }

        private void PlayModeChanged(PlayModeStateChange _)
        {
            _registry = null;
            Refresh();
        }

        public void CreateGUI()
        {
            _refreshPending = false;
            if (_registry == null) _registry = BalanceInventory.Build();
            RememberScroll();
            rootVisualElement.Clear();
            rootVisualElement.AddToClassList("balance-root");
            rootVisualElement.EnableInClassList("balance-light", !EditorGUIUtility.isProSkin);
            StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UI_PATH + ".uss");
            if (!rootVisualElement.styleSheets.Contains(sheet)) rootVisualElement.styleSheets.Add(sheet);
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UI_PATH + ".uxml").CloneTree(rootVisualElement);
            minSize = new Vector2(960, 550);
            rootVisualElement.Q<Label>("draft-status").text = "Draft · " + state.Draft.Changes.Count + " changes";
            ToolbarActions(rootVisualElement.Q<Toolbar>("actions"));
            Navigation(rootVisualElement.Q<Toolbar>("navigation"));
            Messages(rootVisualElement.Q("messages"));
            Workspace(rootVisualElement.Q("workspace"));
            RestoreScroll();
            hasUnsavedChanges = state.Draft.Changes.Count != 0;
            Persist();
        }

        private void Navigation(Toolbar tabs)
        {
            foreach (BalanceTab tab in new[] { BalanceTab.Units, BalanceTab.Compare, BalanceTab.Combat, BalanceTab.Changes })
            {
                ToolbarButton button = new ToolbarButton(() => _controller.ShowTab(tab)) { text = tab.ToString(), name = "workflow-" + tab };
                button.AddToClassList("balance-workflow");
                button.EnableInClassList("balance-tab-active", state.Tab == tab);
                tabs.Add(button);
            }

            Label inventory = new Label(_registry.Units.Count + " units · " + _registry.Fields.Count + " fields");
            inventory.AddToClassList("balance-inventory");
            tabs.Add(inventory);
        }

        private void Messages(VisualElement messages)
        {
            if (_message.Length != 0) messages.Add(new HelpBox(_message, HelpBoxMessageType.Info));
            foreach (string error in _registry.Errors) messages.Add(new HelpBox(error, HelpBoxMessageType.Error));
        }

        private void Workspace(VisualElement body)
        {
            VisualElement centre = new VisualElement();
            centre.AddToClassList("balance-content");
            _details = null;
            if (ShowsDetails())
            {
                TwoPaneSplitView inner = new TwoPaneSplitView(1, state.RightWidth, TwoPaneSplitViewOrientation.Horizontal) { viewDataKey = "balance-details" };
                body.Add(inner);
                inner.Add(centre);
                ScrollView details = new ScrollView { viewDataKey = "balance-field-inspector" };
                details.AddToClassList("balance-inspector");
                details.style.minWidth = 250;
                inner.Add(details);
                _details = details;
                details.RegisterCallback<GeometryChangedEvent>(evt => state.RightWidth = evt.newRect.width);
                if (_registry.Fields.TryGetValue(state.SelectedField, out BalanceField selected)) Details(selected);
            }
            else body.Add(centre);

            switch (state.Tab)
            {
                case BalanceTab.Units:
                    BalanceUnitView.Build(centre, _registry, _controller);
                    break;
                case BalanceTab.Combat:
                    BalanceCombatView.Build(centre, _registry, _controller);
                    break;
                case BalanceTab.Compare:
                    BalanceCompareView.Build(centre, _registry, _controller, false);
                    break;
                case BalanceTab.Changes:
                    BalanceChangesView.Build(centre, _registry, _controller, Apply, RestorePrevious);
                    break;
                default:
                    Tool(centre);
                    break;
            }
        }

        private bool ShowsDetails() => state.ShowDetails
            && state.Tab != BalanceTab.Compare
            && (state.Tab != BalanceTab.Units || state.UnitDetailsOpen)
            && !(state.Tab == BalanceTab.Combat && state.CombatTab == BalanceCombatTab.Hardpoints);

        private void Tool(VisualElement centre)
        {
            Label title = new Label(ObjectNames.NicifyVariableName(state.Tab.ToString()));
            title.AddToClassList("balance-unit-name");
            centre.Add(title);
            var fields = _registry.Fields.Values.Where(field => !field.SharedMountSource && (state.Tab == BalanceTab.AbilityCatalog
                ? field.Group == BalanceFieldGroup.Abilities
                : field.Group == BalanceFieldGroup.GlobalData && !field.Stat.StartsWith("matrix/") && field.Stat != "missSpread"));
            BalanceUnitView.Fields(centre, fields, _controller, "tools-" + state.Tab, new BalanceDraftUsage(_registry, state.Draft));
        }

        private void Apply() => Run(() =>
        {
            _registry = BalanceApplyService.Apply(state.Draft, BalanceInventory.Build);
            _message = "Applied, saved, imported and read back. Previous apply can be restored.";
        });

        private void RestorePrevious() => Run(() =>
        {
            BalanceApplyService.RestorePrevious();
            _registry = BalanceInventory.Build();
            _message = "Previous apply restored and imported.";
        });

        private void ToolbarActions(Toolbar toolbar)
        {
            toolbar.Add(new Label("Preset"));
            ObjectField picker = new ObjectField { objectType = typeof(BalancePreset), value = preset, allowSceneObjects = false, name = "preset-picker" };
            picker.RegisterValueChangedCallback(evt =>
            {
                preset = (BalancePreset)evt.newValue;
                Refresh();
            });
            toolbar.Add(picker);

            ToolbarMenu presets = new ToolbarMenu { text = "Preset actions" };
            toolbar.Add(presets);
            presets.menu.AppendAction("Load into draft", _ => Run(() => BalancePresetService.Load(preset, _registry, state.Draft)),
                _ => preset == null ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
            presets.menu.AppendAction("Save preset", _ => Run(() => SavePreset(false)));
            presets.menu.AppendAction("Save as…", _ => Run(() => SavePreset(true)));
            presets.menu.AppendSeparator();
            foreach (BalancePresetScope scope in Enum.GetValues(typeof(BalancePresetScope)))
            {
                presets.menu.AppendAction("Scope/" + ObjectNames.NicifyVariableName(scope.ToString()), _ => presetScope = scope,
                    _ => presetScope == scope ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }

            ToolbarMenu draft = new ToolbarMenu { text = "Draft" };
            toolbar.Add(draft);
            draft.menu.AppendAction("Undo", _ => _controller.Undo(),
                _ => state.Draft.UndoStates.Count == 0 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
            draft.menu.AppendAction("Redo", _ => _controller.Redo(),
                _ => state.Draft.RedoStates.Count == 0 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
            draft.menu.AppendSeparator();
            draft.menu.AppendAction("Discard draft", _ => _controller.DiscardDraft(),
                _ => state.Draft.Changes.Count == 0 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

            ToolbarMenu tools = new ToolbarMenu { text = "Tools" };
            toolbar.Add(tools);
            tools.menu.AppendAction("Global data", _ => _controller.ShowTab(BalanceTab.GlobalData));
            tools.menu.AppendAction("Ability catalog", _ => _controller.ShowTab(BalanceTab.AbilityCatalog));
            tools.menu.AppendAction("Refresh inventory", _ =>
            {
                _registry = BalanceInventory.Build();
                Refresh();
            });

            VisualElement spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            toolbar.Add(spacer);
            ToolbarButton review = new ToolbarButton(() =>
            {
                _registry = BalanceInventory.Build();
                _controller.ShowTab(BalanceTab.Changes);
            }) { text = "Review & Apply", name = "review-apply" };
            review.AddToClassList("balance-primary");
            toolbar.Add(review);
        }

        private void Details(BalanceField field)
        {
            BalanceDraftUsage usage = new BalanceDraftUsage(_registry, state.Draft);
            BalanceFieldView.Details(_details, field, usage);
            _details.Insert(0, new Button(_controller.CloseField) { text = "Close field details" });
            if (field.SourceKey.Length != 0)
            {
                BalanceField source = _registry.Fields[field.SourceKey];
                _details.Add(new Button(() => _controller.ShowField(source)) { text = "Edit shared nested source (explicit shared action)" });
            }

            if (field.SharedMountSource)
            {
                _details.Add(new HelpBox("This action edits the shared source. Only consumers without a local override inherit the change.", HelpBoxMessageType.Warning));
                _details.Add(BalanceFieldView.Create(field, state.Draft, usage, value => _controller.Edit(field, value), () => { }));
            }
        }

        private void Refresh()
        {
            if (_refreshPending) return;
            _refreshPending = true;
            rootVisualElement.schedule.Execute(CreateGUI);
        }

        private void Run(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                _message = exception.Message;
            }
            Refresh();
        }

        private bool SavePreset(bool saveAs)
        {
            if (preset == null || saveAs)
            {
                BalancePresetService.EnsureFolder();
                string path = EditorUtility.SaveFilePanelInProject("Save balance preset", "BalancePreset", "asset",
                    "Save the staged scope as an Editor-only preset.", BalancePresetService.PRESET_FOLDER);
                if (path.Length == 0) return false;
                preset = CreateInstance<BalancePreset>();
                BalancePresetService.Save(preset, _registry, state.Draft, presetScope);
                AssetDatabase.CreateAsset(preset, path);
                AssetDatabase.SaveAssets();
            }
            else BalancePresetService.Save(preset, _registry, state.Draft, presetScope);

            _message = "Preset saved. Gameplay assets still contain current values.";
            return true;
        }

        public override void SaveChanges()
        {
            if (!SavePreset(false)) return;
            base.SaveChanges();
            File.Delete(RECOVERY_PATH);
        }

        public override void DiscardChanges()
        {
            state.Draft.Discard();
            base.DiscardChanges();
            Persist();
        }

        private static string ScrollKey(ScrollView scroll) =>
            scroll.GetFirstAncestorOfType<ListView>() is ListView list ? list.viewDataKey : scroll.viewDataKey;

        private void RememberScroll()
        {
            foreach (ScrollView scroll in rootVisualElement.Query<ScrollView>().ToList())
            {
                if (!(scroll.contentViewport.resolvedStyle.height > 0)) continue;
                string key = ScrollKey(scroll);
                if (key.Length == 0) continue;
                BalanceScrollPosition position = state.ScrollPositions.FirstOrDefault(entry => entry.Key == key);
                if (position == null)
                {
                    position = new BalanceScrollPosition { Key = key };
                    state.ScrollPositions.Add(position);
                }
                position.Offset = scroll.scrollOffset;
            }
        }

        private void RestoreScroll()
        {
            foreach (ScrollView scroll in rootVisualElement.Query<ScrollView>().ToList())
            {
                BalanceScrollPosition position = state.ScrollPositions.FirstOrDefault(entry => entry.Key == ScrollKey(scroll));
                // Virtualized lists restore their own view data and measure rows before applying our saved offset.
                if (position != null) scroll.schedule.Execute(() => scroll.scrollOffset = position.Offset).ExecuteLater(100);
            }
        }

        private void Persist()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RECOVERY_PATH));
            File.WriteAllText(RECOVERY_PATH, JsonUtility.ToJson(state));
        }
    }
}
