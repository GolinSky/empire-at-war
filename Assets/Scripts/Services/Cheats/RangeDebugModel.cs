namespace EmpireAtWar.Services.Cheats
{
    // Cheat toggle: draw attack and radar range rings around selected units.
    public sealed class RangeDebugModel : IRangeDebugObserver
    {
        public bool IsEnabled { get; set; }
    }
}
