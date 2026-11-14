using EmpireAtWar.Services.Saves;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IStructureKindFacade : IEntityFacade
    {
        StructureKind Kind { get; }
    }
}
