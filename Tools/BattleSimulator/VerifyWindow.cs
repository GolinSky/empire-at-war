using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using EmpireAtWar.Editor;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Editor.BattleSimulator;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public static class VerifyWindow
{
    public static async Task<object> Run()
    {
        EditorHubWindow.OpenTool(BattleSimulatorWindow.MENU_PATH);
        var hub = EditorWindow.GetWindow<EditorHubWindow>();
        await Task.Delay(300);
        var root = hub.rootVisualElement.Q(className: "sim-root");
        if (root == null) throw new InvalidOperationException("Missing embedded simulator.");
        var setupView = root.Q<BattleSimulatorSetupView>();
        var field = typeof(BattleSimulatorSetupView).GetField("_setup", BindingFlags.NonPublic | BindingFlags.Instance);
        var setup = (BattleSetup)field.GetValue(setupView);
        setup.SideA = new List<BattleFleetEntry> { new BattleFleetEntry { UnitId = "Republic/ships/6" } };
        setup.SideB = new List<BattleFleetEntry> { new BattleFleetEntry { UnitId = "Republic/ships/6" } };
        setup.Runs = 10; setup.UseDraft = false;
        using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
        {
            submit.target = root.Q<Button>("sim-run"); submit.target.SendEvent(submit);
        }
        for (int i = 0; i < 100 && !root.Q<Button>("sim-run").enabledSelf; i++) await Task.Delay(100);
        Label summary = root.Q<Label>("sim-summary");
        if (summary == null || !summary.text.StartsWith("10 completed")) throw new InvalidOperationException(root.Q<Label>("sim-status").text);
        if (root.Q<BattleSimulatorTimelineChart>().layout.height < 100) throw new InvalidOperationException("Chart has no layout.");
        if (root.Q<Label>("sim-side-a").worldBound.xMax > root.Q<Label>("sim-side-b").worldBound.xMin)
            throw new InvalidOperationException("Fleet columns overlap.");
        return new { Status = "passed", Summary = summary.text, ChartHeight = root.Q<BattleSimulatorTimelineChart>().layout.height,
            Width = root.layout.width, Note = "Ran 10 live-asset seeds through the embedded Run button; results and chart rendered." };
    }
}
