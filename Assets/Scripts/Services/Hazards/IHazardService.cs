using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Map.Hazards;
using UnityEngine;

namespace EmpireAtWar.Services.Hazards
{
    public interface IHazardService
    {
        event Action<IEntity, SpaceHazardData> Entered;
        IReadOnlyList<IHazardSource> Sources { get; }
        bool BlocksReinforcement(Vector3 position);
        bool TryGetHazardAt(Vector3 position, out SpaceHazardData data);
        float GetPathCost(Vector3 position);
    }
}
