namespace EmpireAtWar.Entities.EnemyFaction.Models.Combat
{
    /// <summary>Per-difficulty hit-and-run thresholds, read from the settings asset.</summary>
    public interface IEnemyHitAndRunSettings
    {
        EnemyHitAndRunTuning Get(EnemyAiDifficulty difficulty);
    }
}
