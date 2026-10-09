using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmpireAtWar.Editor
{
    public sealed class EditorHubWindow : EditorWindow
    {
        private const string STYLE_PATH = "Assets/Scripts/Editor/EditorHub.uss";
        [SerializeField] private string category = "Units";
        [SerializeField] private string selectedMenu = "";
        [SerializeField] private string search = "";
        private IReadOnlyList<EditorToolEntry> _tools;
        private EditorToolWindowHost _host;
        private VisualElement _tabs;
        private ScrollView _list;
        private VisualElement _content;

        [MenuItem(EditorToolCatalog.HUB_MENU_PATH, false, 0)]
        public static void Open() => GetWindow<EditorHubWindow>("Empire At War").Show();

        public static void OpenTool(string menuPath)
        {
            EditorHubWindow window = GetWindow<EditorHubWindow>("Empire At War");
            EditorToolEntry tool = EditorToolCatalog.Discover().Single(entry => entry.MenuPath == menuPath);
            window.category = tool.Category;
            window.selectedMenu = menuPath;
            window.search = "";
            window.CreateGUI();
            window.Show();
        }

        private void OnEnable() => _host = new EditorToolWindowHost();
        private void OnDisable() => _host.Dispose();
        private void OnInspectorUpdate()
        {
            if (_content != null) UpdateState();
        }

        public void CreateGUI()
        {
            minSize = new Vector2(1280, 650);
            _tools = EditorToolCatalog.Discover();
            if (!_tools.Any(tool => tool.Category == category)) category = _tools[0].Category;
            VisualElement root = rootVisualElement;
            root.Clear();
            root.styleSheets.Clear();
            root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(STYLE_PATH));
            root.AddToClassList("editor-hub");
            root.Add(new Label("EMPIRE AT WAR  /  EDITOR HUB") { name = "hub-title" });
            _tabs = new VisualElement { name = "hub-tabs" };
            root.Add(_tabs);
            var workspace = new VisualElement { name = "hub-workspace" };
            root.Add(workspace);
            var sidebar = new VisualElement { name = "hub-sidebar" };
            workspace.Add(sidebar);
            var searchField = new ToolbarSearchField { name = "hub-search", value = search, tooltip = "Search tools in this category" };
            searchField.RegisterValueChangedCallback(evt => { search = evt.newValue; RefreshList(); });
            sidebar.Add(searchField);
            _list = new ScrollView { name = "hub-list" };
            sidebar.Add(_list);
            _content = new VisualElement { name = "hub-content" };
            workspace.Add(_content);
            RefreshTabs();
            RefreshList();
        }

        private void RefreshTabs()
        {
            _tabs.Clear();
            foreach (string tab in _tools.Select(tool => tool.Category).Distinct())
            {
                var button = new Button(() => { category = tab; search = ""; rootVisualElement.Q<ToolbarSearchField>().SetValueWithoutNotify(""); RefreshTabs(); RefreshList(); })
                { text = tab == "Legacy" ? "Legacy / Outdated" : tab, name = "tab-" + tab.Replace(" ", "-") };
                button.EnableInClassList("hub-selected", category == tab);
                _tabs.Add(button);
            }
        }

        private void RefreshList()
        {
            _list.Clear();
            var visible = _tools.Where(tool => tool.Category == category
                && (tool.Title + " " + tool.Group + " " + tool.Description).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            foreach (var group in visible.GroupBy(tool => tool.Group))
            {
                _list.Add(new Label(group.Key) { name = "hub-group" });
                foreach (EditorToolEntry tool in group)
                {
                    var button = new Button(() => { selectedMenu = tool.MenuPath; RefreshList(); })
                    { text = tool.Title, tooltip = tool.Description };
                    button.EnableInClassList("hub-selected", selectedMenu == tool.MenuPath);
                    _list.Add(button);
                }
            }
            EditorToolEntry selected = visible.FirstOrDefault(tool => tool.MenuPath == selectedMenu);
            _content.Clear();
            if (selected == null)
            {
                _content.Add(new Label(visible.Length == 0 ? "No matching tools." : "Select a tool to begin.") { name = "hub-empty" });
                return;
            }
            _content.Add(new Label(selected.Title) { name = "hub-tool-title" });
            _content.Add(new HelpBox(selected.Description, selected.IsLegacy ? HelpBoxMessageType.Warning : HelpBoxMessageType.Info));
            if (selected.WindowType != null)
                _content.Add(_host.GetContent(selected.WindowType));
            else
            {
                var run = new Button(() =>
                {
                    if (selected.IsEnabled && !EditorApplication.ExecuteMenuItem(selected.MenuPath))
                        Debug.LogError("Could not execute editor command: " + selected.MenuPath);
                }) { text = "Run " + selected.Title, name = "hub-run" };
                run.SetEnabled(selected.IsEnabled);
                _content.Add(run);
                _content.Add(new Label("Uses the existing command. Check the Console for its result.") { name = "hub-hint" });
            }
            UpdateState();
        }

        private void UpdateState()
        {
            hasUnsavedChanges = _host.HasUnsavedChanges;
            saveChangesMessage = _host.SaveChangesMessage;
            EditorToolEntry selected = _tools.FirstOrDefault(tool => tool.MenuPath == selectedMenu);
            Button run = _content.Q<Button>("hub-run");
            if (run != null && selected != null) run.SetEnabled(selected.IsEnabled);
        }

        public override void SaveChanges()
        {
            _host.SaveChanges();
            if (!_host.HasUnsavedChanges) base.SaveChanges();
        }

        public override void DiscardChanges()
        {
            _host.DiscardChanges();
            base.DiscardChanges();
        }
    }
}
