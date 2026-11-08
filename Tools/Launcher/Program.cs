using Velopack;

namespace EmpireAtWar.Launcher;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Must run first: handles Velopack install/update/uninstall hooks and exits for them.
        VelopackApp.Build().Run();
        ApplicationConfiguration.Initialize();
        Application.Run(new LauncherForm(new LauncherLog()));
    }
}
