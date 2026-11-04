using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.ShipSpawning
{
    /// <summary>Finds arrival points along the map edges, one edge per spawn group of a batch.</summary>
    public interface IEdgeSpawnPoints
    {
        /// <summary>Starts a new batch: forgets its spacing reservations and picks a new first edge.</summary>
        void BeginBatch();

        bool TryGet(PlayerId owner, UnitTypeId unit, int group, out Vector3 position);
    }
}
