using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    public interface IEnemyStructurePlacementService
    {
        bool TryGetPosition(out Vector3 position);
    }
}
