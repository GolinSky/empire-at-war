using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Services.Battle
{
    public readonly struct SelectionEntry
    {
        public IEntity Entity { get; }
        public IEntitySelectionFacade Command { get; }

        public SelectionEntry(IEntity entity, IEntitySelectionFacade command)
        {
            Entity = entity;
            Command = command;
        }
    }
}
