using System.IO;
using System.Linq;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Services.Battle;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>
    /// Survival wave progress. The director's queue is restored with the model; otherwise its wave counter (0)
    /// would differ from the model's wave and the whole wave would be queued again.
    /// </summary>
    public sealed class SurvivalSaveSource : IBattleSaveSource
    {
        private readonly SurvivalModel _model;
        private readonly SurvivalWaveDirector _director;

        public SurvivalSaveSource(SurvivalModel model, SurvivalWaveDirector director)
        {
            _model = model;
            _director = director;
        }

        public void Capture(BattleSave save) => save.Survival = SurvivalSave.Create(_model.WaveIndex, _model.Phase,
            _model.PreparationTimeLeft, _director.Wave, _director.Pending.Select(UnitTypeSave.From).ToArray(),
            _director.Spawned, _director.SettleTime);

        public void RestoreBeforeUnits(BattleSave save)
        {
            SurvivalSave survival = save.Survival;
            if (survival == null) throw new InvalidDataException("A Survival battle was loaded from a save without wave progress.");
            _model.Restore(survival.WaveIndex, survival.Phase, survival.PreparationTimeLeft);
            _director.Restore(survival.DirectorWave, survival.Pending.Select(unit => unit.UnitType),
                survival.Spawned, survival.SettleTime);
        }

        public void RestoreAfterUnits(BattleRestoreContext context) => _director.AdoptLivingAttackers();
    }
}
