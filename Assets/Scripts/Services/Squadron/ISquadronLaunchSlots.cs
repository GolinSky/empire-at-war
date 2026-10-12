namespace EmpireAtWar.Services.Squadrons
{
    /// <summary>
    /// One reserve launch at a time across every hangar, spaced by a short delay. A squadron spawn costs several
    /// milliseconds, so hangars whose timers expire together launch one after another instead of in one long frame.
    /// </summary>
    public interface ISquadronLaunchSlots
    {
        bool IsAvailable { get; }

        void Take();
    }
}
