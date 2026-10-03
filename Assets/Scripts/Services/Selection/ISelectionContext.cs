using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Services.Selection;

namespace EmpireAtWar.Services.Battle
{
    public interface ISelectionContext
    {
        IEntity Entity { get; }
        IReadOnlyList<IEntity> Entities { get; }
        IEntitySelectionFacade SelectionFacade { get; }
        SelectionType SelectionType { get; }
        bool HasSelectable { get; }
        int Count { get; }
        SelectionScope Scope { get; }

        bool Contains(IEntity entity);
    }
}
