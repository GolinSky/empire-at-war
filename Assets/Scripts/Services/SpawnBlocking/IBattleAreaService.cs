using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.OwnedAreas;
using UnityEngine;

namespace EmpireAtWar.Services.SpawnBlocking
{
    public interface IBattleAreaService
    {
        IReadOnlyList<OwnedCircle> Get(BattleAreaLayer layer);

        bool IsBlocked(PlayerId team, Vector3 position);

        bool IsStructureExcluded(Vector3 position, float margin);

        bool TryGetOrderSnap(Vector3 position, out Vector3 center);
    }
}
