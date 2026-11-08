namespace EmpireAtWar.Launcher;

/// <summary>Appends to launcher.log beside the game's persistentDataPath, outside the replaceable install folder.</summary>
internal sealed class LauncherLog
{
    private const string COMPANY_NAME = "GolinSkyCompany";
    private const string PRODUCT_NAME = "EmpireAtWar";
    private const string FILE_NAME = "launcher.log";

    private readonly string _path;

    public LauncherLog()
    {
        string localLow = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow");
        string directory = Path.Combine(localLow, COMPANY_NAME, PRODUCT_NAME);
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, FILE_NAME);
    }

    public void Write(string message)
    {
        File.AppendAllText(_path, $"{DateTime.UtcNow:O} {message}{Environment.NewLine}");
    }
}
