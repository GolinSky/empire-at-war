using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EmpireAtWar.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public static class VerifyEditorHub
{
    public static async Task<object> Run()
    {
        var tools = EditorToolCatalog.Discover();
        Require(tools.Count >= 34, "Expected the complete project tool catalog.");
        Require(tools.Select(tool => tool.MenuPath).Distinct().Count() == tools.Count, "Duplicate tool paths.");
        Require(tools.Count(tool => tool.IsLegacy) == 5, "Expected five preserved legacy actions.");
        string[] oldRoots = { "Custom/", "CUSTOM/", "Tools/AI/", "Tools/Ships/", "Tools/Squadrons/",
            "Tools/Rendering/", "Tools/Render Audit/", "Tools/Performance/", "Tools/Scene Selector/",
            "Tools/Asset Mapping/", "Tools/Auto App Startup/", "Tools/Volumetric Nebula/", "Tools/Game Design/" };
        var registered = TypeCache.GetMethodsWithAttribute<MenuItem>()
            .SelectMany(method => method.GetCustomAttributes<MenuItem>()).ToArray();
        Require(!registered.Any(item => oldRoots.Any(root => item.menuItem.StartsWith(root, StringComparison.Ordinal))), "Old menu roots remain.");
        Require(registered.All(item => item.menuItem != "Tools/Generate Ship Icons"), "Old icon menu remains.");
        Require(EditorApplication.ExecuteMenuItem(EditorToolCatalog.HUB_MENU_PATH), "Hub menu did not open.");
        var hub = EditorWindow.GetWindow<EditorHubWindow>();
        foreach (EditorToolEntry tool in tools)
        {
            EditorHubWindow.OpenTool(tool.MenuPath);
            if (tool.WindowType != null) await Task.Delay(300);
            Require(hub.rootVisualElement.Q<Label>("hub-tool-title").text == tool.Title, "Wrong tool displayed: " + tool.MenuPath);
            Button run = hub.rootVisualElement.Q<Button>("hub-run");
            Require(tool.WindowType == null ? run != null && run.enabledSelf == tool.IsEnabled : run == null,
                "Wrong command or embedded window state: " + tool.MenuPath);
        }
        var selected = tools.First(tool => tool.Category == "Units");
        EditorHubWindow.OpenTool(selected.MenuPath);
        hub.rootVisualElement.Q<ToolbarSearchField>().value = "no-matching-editor-tool-123";
        Require(hub.rootVisualElement.Q<Label>("hub-empty").text == "No matching tools.", "Search did not filter.");
        hub.rootVisualElement.Q<ToolbarSearchField>().value = "";
        Require(hub.rootVisualElement.Q<Label>("hub-tool-title").text == selected.Title, "Search lost selection.");
        Button legacyTab = hub.rootVisualElement.Q<Button>("tab-Legacy");
        using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
        {
            submit.target = legacyTab;
            legacyTab.SendEvent(submit);
        }
        Require(hub.rootVisualElement.Q<Button>("tab-Legacy").ClassListContains("hub-selected"), "Category tab navigation failed.");
        var embedded = tools.FirstOrDefault(tool => tool.Category == "Game Design");
        if (embedded != null)
        {
            EditorHubWindow.OpenTool(embedded.MenuPath);
            VisualElement original = hub.rootVisualElement.Q(className: "balance-root");
            Require(original != null, "Balance UI was not embedded.");
            EditorHubWindow.OpenTool(selected.MenuPath);
            EditorHubWindow.OpenTool(embedded.MenuPath);
            Require(ReferenceEquals(original, hub.rootVisualElement.Q(className: "balance-root")), "Navigation recreated the editor and lost its state.");
        }
        else EditorHubWindow.OpenTool(selected.MenuPath);
        hub.position = new Rect(40, 60, 1400, 820);
        hub.Focus();
        await Task.Delay(700);
        VisualElement content = hub.rootVisualElement.Q("hub-content");
        Require(content.layout.width > 900 && content.layout.height > 400, "Hub content layout did not resolve.");
        return new { Status = "passed", Tools = tools.Count, LegacyActions = tools.Count(tool => tool.IsLegacy),
            Categories = tools.Select(tool => tool.Category).Distinct().ToArray(), EmbeddedBalance = embedded != null,
            ContentWidth = content.layout.width, ContentHeight = content.layout.height,
            Note = "Navigation and command validation only; no authoring, capture or legacy commands executed." };
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
