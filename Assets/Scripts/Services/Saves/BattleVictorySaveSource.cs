using System.Linq;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>
    /// Which stations the victory rule has seen alive. Without it, a player whose station fell before the save
    /// would never count as defeated.
    /// </summary>
    public sealed class BattleVictorySaveSource : IBattleSaveSource
    {
        private readonly BattleVictoryModel _victory;

        public BattleVictorySaveSource(BattleVictoryModel victory)
        {
            _victory = victory;
        }

        public void Capture(BattleSave save) =>
            save.ObservedBases = _victory.ObservedBases.Select(player => player.Index).OrderBy(index => index).ToArray();

        public void RestoreBeforeUnits(BattleSave save) =>
            _victory.RestoreObservedBases(save.ObservedBases.Select(index => new PlayerId(index)));

        public void RestoreAfterUnits(BattleRestoreContext context)
        {
        }
    }
}
