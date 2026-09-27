using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Services.Battle
{
    public readonly struct SelectionEntry
    {
        public SelectionEntry(IEntity entity, IEntitySelectionFacade command)
        {
            Entity = entity;
            Command = command;
        }

        public IEntity Entity { get; }
        public IEntitySelectionFacade Command { get; }
    }
}
