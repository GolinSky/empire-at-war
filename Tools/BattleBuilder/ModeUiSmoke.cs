using System.IO;
using EmpireAtWar.Entities.MainMenu.Simulation;
using EmpireAtWar.Services.VContainer;
using UnityEngine;
using VContainer;

public static class ModeUiSmoke
{
    public static string OpenSetup()
    {
        Object.FindAnyObjectByType<MainMenuScope>().Container.Resolve<ISimulationRoute>().Open();
        return "Opened Simulation setup.";
    }
    public static string Capture()
    {
        Directory.CreateDirectory("output/BattleBuilderPhase1");
        ScreenCapture.CaptureScreenshot("output/BattleBuilderPhase1/mode-ui.png");
        return "Screenshot requested for next rendered frame.";
    }
}
