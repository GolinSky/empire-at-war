using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.TacticalMarkers;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class AllyAiMarkerObjectiveTests
    {
        [Test]
        public void CaptureRequestKeepsRealObjectiveIdentityAndCaptureCircle()
        {
            var objective = new EnemyCaptureObjective("site:4", EnemyCaptureObjectiveType.Facility,
                TestPlayers.Enemy, new Vector3(300, 0, 200), 123f, 0.25f);
            var objectives = new List<EnemyCaptureObjective> { objective };
            var marker = new TacticalMarker(7, TacticalMarkerType.Capture, TestPlayers.Human,
                new TeamId(0), objective.Position, -1, 0, 8);
            AllyAiMarkerObjective.Apply(objectives, marker, TestPlayers.Ally);
            Assert.That(objectives.Count, Is.EqualTo(1));
            Assert.That(objectives[0].Id, Is.EqualTo(objective.Id));
            Assert.That(objectives[0].Radius, Is.EqualTo(objective.Radius));
            Assert.That(objectives[0].Owner, Is.EqualTo(objective.Owner));
            Assert.That(objectives[0].Progress, Is.EqualTo(objective.Progress));
            Assert.That(objectives[0].PriorityBonus, Is.GreaterThan(objective.PriorityBonus));
        }

        [Test]
        public void PointRequestDoesNotInventAnArrivalCircle()
        {
            var objectives = new List<EnemyCaptureObjective>();
            var marker = new TacticalMarker(7, TacticalMarkerType.Capture, TestPlayers.Human,
                new TeamId(0), new Vector3(300, 0, 200), -1, 0, 8);
            AllyAiMarkerObjective.Apply(objectives, marker, TestPlayers.Ally);
            Assert.That(objectives[0].Position, Is.EqualTo(marker.Point));
            Assert.That(objectives[0].Radius, Is.Zero);
        }
    }
}
