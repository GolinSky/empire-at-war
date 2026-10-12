namespace EmpireAtWar.Services.Squadrons
{
    /// <summary>
    /// One reserve launch per frame across every hangar. A squadron spawn costs several milliseconds,
    /// so hangars whose timers expire together launch on consecutive frames instead of one long frame.
    /// </summary>
    public interface ISquadronLaunchSlots
    {
        bool IsAvailable { get; }

        void Take();
    }
}
