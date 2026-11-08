using EmpireAtWar.Services.Saves;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    /// <summary>Saves and restores the parts of a ship or squadron that only the unit itself can reach.</summary>
    public interface IUnitSaveFacade : IEntityFacade
    {
        UnitSave Capture();

        void RestoreHealth(UnitHealthSave save);

        /// <summary>Online client: a host health update, with destruction effects for parts lost since the last.</summary>
        void ApplyReplicaHealth(UnitHealthSave save);
    }
}
