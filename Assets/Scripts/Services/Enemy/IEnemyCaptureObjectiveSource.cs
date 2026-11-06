using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;

namespace EmpireAtWar.Services.Enemy
{
    public interface IEnemyCaptureObjectiveSource
    {
        void AddStrategicObjectives(List<EnemyCaptureObjective> objectives, HostileIntelModel intel);
        void AddSquadronTargets(EnemySquadronCaptureAssignments assignments);
    }
}
