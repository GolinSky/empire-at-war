using EmpireAtWar.Editor.Rendering;

public static class BuildRepublicStationWreck
{
    public static string Main()
    {
        return ShipWreckBuilder.Build("Assets/Prefabs/Models/Stations/Republic/RepublicSpaceStationView.prefab").name;
    }
}
