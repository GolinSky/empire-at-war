namespace EmpireAtWar.Services.Saves
{
    /// <summary>
    /// A system that saves its own state into the battle save. Modes and modules register one only when the
    /// system exists, so a disabled feature adds nothing.
    /// </summary>
    public interface IBattleSaveSource
    {
        void Capture(BattleSave save);

        /// <summary>Runs before units and structures are spawned, e.g. the station level they spawn at.</summary>
        void RestoreBeforeUnits(BattleSave save);

        /// <summary>Runs once every saved unit and structure exists, e.g. to link them to their owners.</summary>
        void RestoreAfterUnits(BattleRestoreContext context);
    }
}
