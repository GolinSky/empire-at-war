using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IVisionFootprintFacade : IEntityFacade
    {
        long EntityId { get; }
        IReadOnlyList<Vector2> WorldHull { get; }
        Rect WorldBounds { get; }
    }
}
