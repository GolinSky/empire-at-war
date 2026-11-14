using EmpireAtWar.Services.Saves;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public sealed class StructureKindFacade : IStructureKindFacade
    {
        public StructureKind Kind { get; }

        public StructureKindFacade(StructureKind kind) => Kind = kind;
    }
}
