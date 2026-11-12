using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Enemy;

namespace EmpireAtWar.Services.TacticalMarkers
{
    public static class AllyAiMarkerObjective
    {
        private const float PRIORITY_BONUS = 3000f;

        public static void Apply(List<EnemyCaptureObjective> objectives, TacticalMarker marker, PlayerId owner)
        {
            int nearest = -1;
            float distance = AllyAiMarkerSource.REQUEST_RADIUS * AllyAiMarkerSource.REQUEST_RADIUS;
            for (int i = 0; i < objectives.Count; i++)
            {
                float candidateDistance = (objectives[i].Position - marker.Point).sqrMagnitude;
                if (candidateDistance >= distance) continue;
                nearest = i;
                distance = candidateDistance;
            }
            if (nearest >= 0)
            {
                EnemyCaptureObjective objective = objectives[nearest];
                objectives[nearest] = new EnemyCaptureObjective(objective.Id, objective.Kind, objective.Owner,
                    objective.Position, objective.Radius, objective.Progress,
                    objective.IsThreatened || marker.Type == TacticalMarkerType.Defend, PRIORITY_BONUS);
                return;
            }
            // An area request has no capture circle: approach its point rather than stopping at the request radius.
            objectives.Add(new EnemyCaptureObjective($"marker:{marker.Id}", EnemyCaptureObjectiveType.Facility,
                marker.Type == TacticalMarkerType.Defend ? owner : PlayerId.None,
                marker.Point, 0f, 0f, marker.Type == TacticalMarkerType.Defend, PRIORITY_BONUS));
        }
    }
}
