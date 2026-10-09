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
        [SerializeField] private string presetScope = "Full registered set";
        private BalanceRegistration _registry;
        private string _message = "";
        private VisualElement _details;
        private bool _refreshPending;

        [MenuItem(MENU_PATH)]
        public static void Open() => EditorHubWindow.OpenTool(MENU_PATH);

        private void OnEnable()
        {
            if (state.Draft.Changes.Count == 0 && File.Exists(RECOVERY_PATH)) state = JsonUtility.FromJson<BalanceWindowState>(File.ReadAllText(RECOVERY_PATH));
            saveChangesMessage = "Save the staged balance draft as a preset before closing? Gameplay assets are written only through Review/Apply.";
            EditorApplication.playModeStateChanged += PlayModeChanged;
        }

        private void OnDisable() { RememberScroll(); Persist(); EditorApplication.playModeStateChanged -= PlayModeChanged; }
        private void PlayModeChanged(PlayModeStateChange _) { _registry = null; Refresh(); }

        public void CreateGUI()
        {
            _refreshPending = false;
            if (_registry == null) _registry = BalanceInventory.Build();
            if (new[] { "Weapons", "Abilities", "Hardpoints" }.Contains(state.Tab)) { state.UnitTab = state.Tab; state.Tab = "Units"; }
            if (state.SelectedUnit.Length == 0)
                state.SelectedUnit = _registry.Units.First(unit => unit.Kind == state.Kind).Id;
            RememberScroll(); rootVisualElement.Clear(); rootVisualElement.AddToClassList("balance-root");
            rootVisualElement.EnableInClassList("balance-light", !EditorGUIUtility.isProSkin);
            StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UI_PATH + ".uss");
            if (!rootVisualElement.styleSheets.Contains(sheet)) rootVisualElement.styleSheets.Add(sheet);
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UI_PATH + ".uxml").CloneTree(rootVisualElement);
            minSize = new Vector2(960, 550);
            rootVisualElement.Q<Label>("draft-status").text = "Draft · " + state.Draft.Changes.Count + " changes";
            Toolbar actions = rootVisualElement.Q<Toolbar>("actions"); ToolbarActions(actions);
            Toolbar tabs = rootVisualElement.Q<Toolbar>("navigation");
            foreach (string tab in new[] { "Units", "Compare", "Combat", "Changes" })
            {
                ToolbarButton button = new ToolbarButton(() => { state.Tab = tab; Refresh(); }) { text = tab, name = "workflow-" + tab };
                button.AddToClassList("balance-workflow"); button.EnableInClassList("balance-tab-active", state.Tab == tab); tabs.Add(button);
            }
            Label inventory = new Label(_registry.Units.Count + " units · " + _registry.Fields.Count + " fields"); inventory.AddToClassList("balance-inventory"); tabs.Add(inventory);
            VisualElement messages = rootVisualElement.Q("messages");
            if (_message.Length != 0) messages.Add(new HelpBox(_message, HelpBoxMessageType.Info));
            foreach (string error in _registry.Errors) messages.Add(new HelpBox(error, HelpBoxMessageType.Error));
            VisualElement body = rootVisualElement.Q("workspace");
            if (state.Tab != "Compare" && state.Tab != "Combat")
            {
                TwoPaneSplitView outer = new TwoPaneSplitView(0, state.LeftWidth, TwoPaneSplitViewOrientation.Horizontal) { viewDataKey = "balance-left" };
                outer.AddToClassList("balance-workspace"); body.Add(outer); body = outer;
                VisualElement roster = new VisualElement(); roster.style.minWidth = 220; outer.Add(roster);
                roster.RegisterCallback<GeometryChangedEvent>(evt => state.LeftWidth = evt.newRect.width);
                BalanceRosterView.Build(roster, _registry, state, Refresh);
            }
            VisualElement centre = new VisualElement(); centre.AddToClassList("balance-content"); _details = null;
            if (state.ShowDetails && state.Tab != "Compare" && !(state.Tab == "Combat" && state.CombatTab == "Hardpoints"))
            {
                TwoPaneSplitView inner = new TwoPaneSplitView(1, state.RightWidth, TwoPaneSplitViewOrientation.Horizontal) { viewDataKey = "balance-details" };
                body.Add(inner); inner.Add(centre);
                ScrollView details = new ScrollView { viewDataKey = "balance-field-inspector" }; details.AddToClassList("balance-inspector"); details.style.minWidth = 250; inner.Add(details); _details = details;
                details.RegisterCallback<GeometryChangedEvent>(evt => state.RightWidth = evt.newRect.width);
                if (_registry.Fields.TryGetValue(state.SelectedField, out BalanceField selected)) Details(selected);
            }
            else body.Add(centre);
            if (state.Tab == "Units") BalanceUnitView.Build(centre, _registry, state, Edit, Select, Refresh);
            else if (state.Tab == "Combat") BalanceCombatView.Build(centre, _registry, state, Edit, Select, Refresh);
            else if (state.Tab == "Compare")
            {
                BalanceCompareView.Build(centre, _registry, state, Edit, Refresh);
            }
            else if (state.Tab == "Changes") BalanceChangesView.Build(centre, _registry, state,
                () => Run(() => { _registry = BalanceApplyService.Apply(state.Draft, BalanceInventory.Build); _message = "Applied, saved, imported and read back. Previous apply can be restored."; }),
                () => Run(() => { BalanceApplyService.RestorePrevious(); _registry = BalanceInventory.Build(); _message = "Previous apply restored and imported."; }), Refresh);
            else
            {
                Label title = new Label(state.Tab); title.AddToClassList("balance-unit-name"); centre.Add(title);
                BalanceUnitView.Fields(centre, _registry.Fields.Values.Where(field => !field.SharedMountSource && (state.Tab == "Ability catalog"
                    ? field.Group == "Abilities" : field.Group == "Global Data" && !field.Stat.StartsWith("matrix/") && field.Stat != "missSpread")), state, Edit, Select, "tools-" + state.Tab, new BalanceDraftUsage(_registry, state.Draft));
            }
            foreach (ScrollView scroll in rootVisualElement.Query<ScrollView>().ToList())
            {
                BalanceScrollPosition position = state.ScrollPositions.FirstOrDefault(entry => entry.Key == ScrollKey(scroll));
                // Virtualized lists restore their own view data and measure rows before applying our saved offset.
                if (position != null) scroll.schedule.Execute(() => scroll.scrollOffset = position.Offset).ExecuteLater(100);
            }
            hasUnsavedChanges = state.Draft.Changes.Count != 0; Persist();
        }

        private void ToolbarActions(Toolbar toolbar)
        {
            toolbar.Add(new Label("Preset"));
            ObjectField picker = new ObjectField { objectType = typeof(BalancePreset), value = preset, allowSceneObjects = false, name = "preset-picker" };
            picker.RegisterValueChangedCallback(evt => { preset = (BalancePreset)evt.newValue; Refresh(); }); toolbar.Add(picker);
            ToolbarMenu presets = new ToolbarMenu { text = "Preset actions" }; toolbar.Add(presets);
            presets.menu.AppendAction("Load into draft", _ => Run(() => BalancePresetService.Load(preset, _registry, state.Draft)), _ => preset == null ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
            presets.menu.AppendAction("Save preset", _ => Run(() => SavePreset(false)));
            presets.menu.AppendAction("Save as…", _ => Run(() => SavePreset(true)));
            presets.menu.AppendSeparator();
            foreach (string scope in new[] { "Full registered set", "Changed fields only" })
                presets.menu.AppendAction("Scope/" + scope, _ => presetScope = scope, _ => presetScope == scope ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            ToolbarMenu draft = new ToolbarMenu { text = "Draft" }; toolbar.Add(draft);
            draft.menu.AppendAction("Undo", _ => { state.Draft.Undo(); Refresh(); }, _ => state.Draft.UndoStates.Count == 0 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
            draft.menu.AppendAction("Redo", _ => { state.Draft.Redo(); Refresh(); }, _ => state.Draft.RedoStates.Count == 0 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
            draft.menu.AppendSeparator();
            draft.menu.AppendAction("Discard draft", _ => { state.Draft.Discard(); Refresh(); }, _ => state.Draft.Changes.Count == 0 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
            ToolbarMenu tools = new ToolbarMenu { text = "Tools" }; toolbar.Add(tools);
            tools.menu.AppendAction("Global data", _ => { state.Tab = "Global Data"; Refresh(); });
            tools.menu.AppendAction("Ability catalog", _ => { state.Tab = "Ability catalog"; Refresh(); });
            tools.menu.AppendAction("Refresh inventory", _ => { _registry = BalanceInventory.Build(); Refresh(); });
            VisualElement spacer = new VisualElement(); spacer.style.flexGrow = 1; toolbar.Add(spacer);
            ToolbarButton review = new ToolbarButton(() => { _registry = BalanceInventory.Build(); state.Tab = "Changes"; Refresh(); })
                { text = "Review & Apply", name = "review-apply" }; review.AddToClassList("balance-primary"); toolbar.Add(review);
        }

        private void Select(BalanceField field) { state.SelectedField = field.Key; state.ShowDetails = true; Refresh(); }
        private void Details(BalanceField field)
        {
            BalanceDraftUsage usage = new BalanceDraftUsage(_registry, state.Draft);
            BalanceFieldView.Details(_details, field, usage);
            Button close = new Button(() => { state.ShowDetails = false; Refresh(); }) { text = "Close field details" }; _details.Insert(0, close);
            if (field.SourceKey.Length != 0)
                _details.Add(new Button(() => Select(_registry.Fields[field.SourceKey])) { text = "Edit shared nested source (explicit shared action)" });
            if (field.SharedMountSource)
            {
                _details.Add(new HelpBox("This action edits the shared source. Only consumers without a local override inherit the change.", HelpBoxMessageType.Warning));
                _details.Add(BalanceFieldView.Create(field, state, usage, value => Edit(field, value), () => { }));
            }
        }
        private void Edit(BalanceField field, string value) { state.Draft.Set(field.Snapshot(), value); Refresh(); }
        private void Refresh()
        {
            if (_refreshPending) return;
            _refreshPending = true; rootVisualElement.schedule.Execute(CreateGUI);
        }
        private void Run(Action action)
        {
            try { action(); }
            catch (Exception exception) { _message = exception.Message; }
            Refresh();
        }

        private bool SavePreset(bool saveAs)
        {
            if (preset == null || saveAs)
            {
                BalancePresetService.EnsureFolder();
                string path = EditorUtility.SaveFilePanelInProject("Save balance preset", "BalancePreset", "asset", "Save the staged scope as an Editor-only preset.", BalancePresetService.PRESET_FOLDER);
                if (path.Length == 0) return false;
                preset = CreateInstance<BalancePreset>();
                BalancePresetService.Save(preset, _registry, state.Draft, presetScope);
                AssetDatabase.CreateAsset(preset, path); AssetDatabase.SaveAssets();
            }
            else BalancePresetService.Save(preset, _registry, state.Draft, presetScope);
            _message = "Preset saved. Gameplay assets still contain current values."; return true;
        }

        public override void SaveChanges()
        {
            if (SavePreset(false)) { base.SaveChanges(); File.Delete(RECOVERY_PATH); }
        }
        public override void DiscardChanges() { state.Draft.Discard(); base.DiscardChanges(); Persist(); }
        private static string ScrollKey(ScrollView scroll) => scroll.GetFirstAncestorOfType<ListView>() is ListView list ? list.viewDataKey : scroll.viewDataKey;
        private void RememberScroll()
        {
            foreach (ScrollView scroll in rootVisualElement.Query<ScrollView>().ToList())
            {
                if (!(scroll.contentViewport.resolvedStyle.height > 0)) continue;
                string key = ScrollKey(scroll);
                if (key.Length == 0) continue;
                BalanceScrollPosition position = state.ScrollPositions.FirstOrDefault(entry => entry.Key == key);
                if (position == null) { position = new BalanceScrollPosition { Key = key }; state.ScrollPositions.Add(position); }
                position.Offset = scroll.scrollOffset;
            }
        }
        private void Persist()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RECOVERY_PATH));
            File.WriteAllText(RECOVERY_PATH, JsonUtility.ToJson(state));
        }
    }
}
