using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Services.Enemy;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>Wing commander stand-in for production tests: never asks for a patrol wing.</summary>
    public sealed class NoPatrolSquadronCommander : IEnemySquadronCommander
    {
        public bool NeedsPatrolWing => false;

        public void SetStationDefense(Vector3? position) { }

        public void Command(ISquadron squadron, SquadronType squadronType) { }

        public void SetOuterWingCount(int count) { }

        public void SetWingPatrol(bool enabled) { }

        public bool TryGetSpawnBeacon(out Vector3 position)
        {
            position = default;
            return false;
        }
    }
}
