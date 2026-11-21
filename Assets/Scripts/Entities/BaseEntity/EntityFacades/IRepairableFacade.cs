namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    /// <summary>A unit that repairers can restore: every ship and structure with health; squadrons have none.</summary>
    public interface IRepairableFacade : IEntityFacade
    {
        bool NeedsRepair { get; }
        float HullPercentage { get; }

        /// <summary>Restores this fraction of the maximum hull and of each live hardpoint.</summary>
        void Repair(float fraction);
    }
}
