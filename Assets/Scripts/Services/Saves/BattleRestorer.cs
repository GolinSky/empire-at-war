using System.Collections.Generic;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>
    /// Applies a loaded save to a freshly built battle: system state first, then units and structures,
    /// then the links between them.
    /// </summary>
    public sealed class BattleRestorer
    {
        private readonly UnitSaveService _units;
        private readonly IReadOnlyList<IBattleSaveSource> _sources;

        public BattleRestorer(UnitSaveService units, IReadOnlyList<IBattleSaveSource> sources)
        {
            _units = units;
            _sources = sources;
        }

        public void Restore(BattleSave save)
        {
            foreach (IBattleSaveSource source in _sources) source.RestoreBeforeUnits(save);
            BattleRestoreContext context = _units.Restore(save);
            foreach (IBattleSaveSource source in _sources) source.RestoreAfterUnits(context);
        }
    }
}
