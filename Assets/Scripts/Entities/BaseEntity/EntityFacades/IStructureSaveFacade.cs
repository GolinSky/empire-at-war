using EmpireAtWar.Services.Saves;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    /// <summary>Saves and restores a station, mining facility or defense platform.</summary>
    public interface IStructureSaveFacade : IEntityFacade
    {
        StructureSave Capture();

        void RestoreHealth(UnitHealthSave save);
    }
}
