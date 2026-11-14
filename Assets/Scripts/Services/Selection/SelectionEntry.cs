using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Services.Battle
{
    public readonly struct SelectionEntry
    {
        public IEntity Entity { get; }
        public IEntitySelectionFacade Command { get; }
        public int? MarkerDrawOrder { get; }
        public bool IsBattleMarker => MarkerDrawOrder.HasValue;

        public SelectionEntry(IEntity entity, IEntitySelectionFacade command, int? markerDrawOrder = null)
        {
            Entity = entity;
            Command = command;
            MarkerDrawOrder = markerDrawOrder;
        }
    }
}
