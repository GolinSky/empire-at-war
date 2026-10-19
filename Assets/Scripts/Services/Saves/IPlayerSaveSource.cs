namespace EmpireAtWar.Services.Saves
{
    /// <summary>A player-scope system that saves its part of one player's state; see <see cref="PlayerSaveRegistry"/>.</summary>
    public interface IPlayerSaveSource
    {
        void Capture(PlayerSave save);

        void RestoreBeforeUnits(PlayerSave save);

        void RestoreAfterUnits(PlayerSave save, BattleRestoreContext context);
    }
}
