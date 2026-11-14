using EmpireAtWar.Services.Saves;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    /// <summary>Saves and restores a station, mining facility or defense platform.</summary>
    public interface IStructureSaveFacade : IEntityFacade
    {
        StructureSave Capture();

        UnitHealthSave CaptureHealth();

        void RestoreHealth(UnitHealthSave save);

        /// <summary>Online client: a host health update, with destruction effects for parts lost since the last.</summary>
        void ApplyReplicaHealth(UnitHealthSave save);
    }
}
