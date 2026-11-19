using System.Collections.Generic;
using System.Numerics;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Services.ShipNavigation;
using EmpireAtWar.Services.UnitOrders;
using NUnit.Framework;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Navigation")]
    public sealed class PaceClusterBuilderTests
    {
        [Test]
        public void FarApartGroups_FormSeparateClustersAndSingletonGetsNull()
        {
            PaceClusterTuning tuning = PaceClusterTuning.Default;
            float far = tuning.LinkDistance * 3f;
            PaceMemberCandidate[] candidates =
            {
                new PaceMemberCandidate(new Vector2(0f, 0f), 20f, 30f, false),
                new PaceMemberCandidate(new Vector2(100f, 0f), 20f, 25f, false),
                new PaceMemberCandidate(new Vector2(far, 0f), 20f, 30f, false),
                new PaceMemberCandidate(new Vector2(far + 100f, 0f), 20f, 28f, false),
                new PaceMemberCandidate(new Vector2(0f, far * 2f), 20f, 30f, false)
            };

            PaceCluster[] clusters = PaceClusterBuilder.Build(candidates, orderId: 1, tuning);

            Assert.That(clusters[0], Is.Not.Null);
            Assert.That(clusters[0], Is.SameAs(clusters[1]));
            Assert.That(clusters[2], Is.Not.Null);
            Assert.That(clusters[2], Is.SameAs(clusters[3]));
            Assert.That(clusters[0], Is.Not.SameAs(clusters[2]));
            Assert.That(clusters[4], Is.Null);
        }

        [Test]
        public void BlockedSegment_SplitsNearbyShipsIntoSeparateClusters()
        {
            PaceClusterTuning tuning = PaceClusterTuning.Default;
            PaceMemberCandidate[] candidates =
            {
                new PaceMemberCandidate(new Vector2(-100f, 0f), 20f, 30f, false),
                new PaceMemberCandidate(new Vector2(100f, 0f), 20f, 30f, false)
            };

            // Segment crosses x = 0 obstacle.
            PaceCluster[] clusters = PaceClusterBuilder.Build(candidates, orderId: 1, tuning,
                (a, b) => !((a.X < 0f && b.X > 0f) || (b.X < 0f && a.X > 0f)));

            Assert.That(clusters[0], Is.Null);
            Assert.That(clusters[1], Is.Null);
        }

        [Test]
        public void SpeedRatio_SeparatesMismatchedShipsButLinksNearbySquadron()
        {
            PaceClusterTuning tuning = PaceClusterTuning.Default;
            float slowShip = 20f;
            float tooFastShip = slowShip * (tuning.LinkSpeedRatio + 1f);

            PaceMemberCandidate[] mismatchedShips =
            {
                new PaceMemberCandidate(new Vector2(0f, 0f), 20f, slowShip, false),
                new PaceMemberCandidate(new Vector2(50f, 0f), 20f, tooFastShip, false)
            };
            PaceCluster[] shipResult = PaceClusterBuilder.Build(mismatchedShips, orderId: 1, tuning);
            Assert.That(shipResult[0], Is.Null);
            Assert.That(shipResult[1], Is.Null);

            PaceMemberCandidate[] shipAndSquadron =
            {
                new PaceMemberCandidate(new Vector2(0f, 0f), 20f, slowShip, false),
                new PaceMemberCandidate(new Vector2(50f, 0f), 5f, tooFastShip, true)
            };
            PaceCluster[] mixedResult = PaceClusterBuilder.Build(shipAndSquadron, orderId: 2, tuning);
            Assert.That(mixedResult[0], Is.Not.Null);
            Assert.That(mixedResult[0], Is.SameAs(mixedResult[1]));
        }

        [Test]
        public void MergeService_MergesSameOrderClustersAfterConsecutiveChecks_NotDifferentOrders()
        {
            UnitOrderSettings settings = ScriptableObject.CreateInstance<UnitOrderSettings>();
            try
            {
                PaceClusterTuning tuning = settings.PaceTuning;
                var obstacles = new FakeObstacles();
                var mergeService = new PaceClusterMergeService(settings, obstacles);

                PaceCluster a = new PaceCluster(orderId: 1, issuePace: 30f, tuning: tuning);
                a.Join(30f, 500f, new Vector2(0f, 0f), 20f, Vector2.UnitX);
                a.Join(30f, 500f, new Vector2(10f, 0f), 20f, Vector2.UnitX);

                PaceCluster b = new PaceCluster(orderId: 1, issuePace: 25f, tuning: tuning);
                b.Join(25f, 500f, new Vector2(40f, 0f), 20f, Vector2.UnitX);
                b.Join(25f, 500f, new Vector2(50f, 0f), 20f, Vector2.UnitX);

                PaceCluster differentOrder = new PaceCluster(orderId: 2, issuePace: 30f, tuning: tuning);
                differentOrder.Join(30f, 500f, new Vector2(20f, 0f), 20f, Vector2.UnitX);
                differentOrder.Join(30f, 500f, new Vector2(30f, 0f), 20f, Vector2.UnitX);

                mergeService.Register(a);
                mergeService.Register(b);
                mergeService.Register(differentOrder);

                // First check records hold count = 1, does not merge yet.
                mergeService.Tick(tuning.MergeInterval);
                Assert.That(a.IsMerged, Is.False);
                Assert.That(b.IsMerged, Is.False);

                // Second consecutive check merges a and b, leaving differentOrder untouched.
                mergeService.Tick(tuning.MergeInterval);
                Assert.That(a.IsMerged || b.IsMerged, Is.True);
                Assert.That(a.Resolve(), Is.SameAs(b.Resolve()));
                Assert.That(differentOrder.IsMerged, Is.False);
                Assert.That(differentOrder.Resolve(), Is.Not.SameAs(a.Resolve()));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        private sealed class FakeObstacles : IMapObstacleContactProvider
        {
            public string Id => nameof(FakeObstacles);
            public void CopyContacts(List<RadarContact> destination) { }
        }
    }
}
