using System;
using System.IO;
using EmpireAtWar.Services.ShipNavigation;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Navigation")]
    public class ShipTangentFailureSnapshotTests
    {
        [Test]
        public void Save_RetainsFourCompleteReplayInputsIncludingCircleLinks()
        {
            string directory = Path.Combine(Path.GetTempPath(), "ShipTangentFailure-" + Guid.NewGuid());
            try
            {
                var snapshot = new ShipTangentFailureSnapshot
                {
                    Forward = Vector3.forward, TurnRadius = 3f, AllowEscape = true,
                    BoundsMin = new float2(-10f), BoundsMax = new float2(10f),
                    FailureCode = 3, RepairCount = 41, RepairLimit = 40,
                    Agent = new ShipTangentFailureSnapshot.AgentContext
                    {
                        IsAvailable = true, RegistrationId = 17, CurrentSpeed = 8f, Clearance = 2f, Height = 5f
                    },
                    Waypoints = new[] { Vector3.left, Vector3.right },
                    Disks = new[] { new float3(2f, 3f, 4f) },
                    Corners = new[] { new ShipTangentFailureSnapshot.Corner
                    {
                        Position = Vector3.one, DiskIndex = 0, TurnSign = -1, PortalWidth = 0.5f
                    } },
                    FinalCircles = new[] { new ShipRouteCircle(new float2(2f, 3f), 4f, -1, 0)
                    {
                        Previous = 2, Next = 3, SweepHint = 1.5f
                    } }
                };
                string latest = null;
                for (int i = 0; i < 6; i++)
                {
                    snapshot.BuildId = i;
                    latest = snapshot.Save(directory);
                }

                Assert.That(Directory.GetFiles(directory), Has.Length.EqualTo(4));
                var restored = JsonUtility.FromJson<ShipTangentFailureSnapshot>(File.ReadAllText(latest));
                Assert.That(restored.BuildId, Is.EqualTo(5));
                Assert.That(restored.Agent.RegistrationId, Is.EqualTo(17));
                Assert.That(restored.Agent.CurrentSpeed, Is.EqualTo(8f));
                Assert.That(restored.Agent.Clearance, Is.EqualTo(2f));
                Assert.That(restored.AllowEscape, Is.True);
                Assert.That(restored.Forward, Is.EqualTo(snapshot.Forward));
                Assert.That(restored.BoundsMin, Is.EqualTo(snapshot.BoundsMin));
                Assert.That(restored.BoundsMax, Is.EqualTo(snapshot.BoundsMax));
                Assert.That(restored.Waypoints, Is.EqualTo(snapshot.Waypoints));
                Assert.That(restored.Disks, Is.EqualTo(snapshot.Disks));
                Assert.That(restored.Corners[0].DiskIndex, Is.EqualTo(0));
                Assert.That(restored.Corners[0].TurnSign, Is.EqualTo(-1));
                Assert.That(restored.Corners[0].PortalWidth, Is.EqualTo(0.5f));
                Assert.That(restored.FinalCircles, Is.EqualTo(snapshot.FinalCircles));
                Assert.That(restored.RepairCount, Is.EqualTo(41));
                Assert.That(restored.RepairLimit, Is.EqualTo(40));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }
    }
}
