using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.StateMachine;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>
    /// Threat sense for ship-state tests: by default never hit, no attackers, no team vision.
    /// </summary>
    public sealed class FakeShipThreats : IShipThreatSense
    {
        public bool WasDamagedRecently { get; set; }
        public HashSet<IEntity> SelfAttackers { get; } = new HashSet<IEntity>();
        public HashSet<IEntity> StationAttackers { get; } = new HashSet<IEntity>();
        public List<IEntity> TeamVisible { get; } = new List<IEntity>();

        public bool IsAttackingSelf(IEntity hostile) => SelfAttackers.Contains(hostile);

        public bool IsAttackingTeamStation(IEntity hostile) => StationAttackers.Contains(hostile);

        public void FillTeamVisibleHostiles(Vector3 center, float radius, List<IEntity> results) =>
            results.AddRange(TeamVisible);
    }
}
