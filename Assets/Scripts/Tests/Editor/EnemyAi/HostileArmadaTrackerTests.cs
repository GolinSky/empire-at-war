using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Services.Enemy;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class HostileArmadaTrackerTests
    {
        private static readonly UnitCombatProfile SHIP = CreateProfile();

        [Test]
        public void Update_TwoDistantClustersAreTwoArmadas_ASmallGroupIsNone()
        {
            var ships = new List<Vector3>();
            AddCluster(ships, Vector3.zero, HostileArmadaTracker.MIN_SHIPS);
            AddCluster(ships, new Vector3(5000f, 0f, 0f), HostileArmadaTracker.MIN_SHIPS);
            AddCluster(ships, new Vector3(0f, 0f, 5000f), HostileArmadaTracker.MIN_SHIPS - 1);
            var tracker = new HostileArmadaTracker();

            tracker.Update(ships, AddShip, Array.Empty<ArmadaAsset>(), 0f);

            Assert.That(tracker.Armadas.Count, Is.EqualTo(2));
            foreach (HostileArmada armada in tracker.Armadas)
            {
                Assert.That(armada.Ships, Is.EqualTo(HostileArmadaTracker.MIN_SHIPS));
                Assert.That(armada.Strength.UnitCount, Is.EqualTo(armada.Ships));
            }
        }

        [Test]
        public void Update_MovingArmadaThreatensTheAssetItHeadsFor()
        {
            var west = new ArmadaAsset(new Vector3(-2500f, 0f, 0f), 0f, ArmadaAssetKind.Station);
            var east = new ArmadaAsset(new Vector3(2500f, 0f, 0f), 0f, ArmadaAssetKind.HeldZone);
            ArmadaAsset[] assets = { west, east };
            var tracker = new HostileArmadaTracker();
            var ships = new List<Vector3>();
            AddCluster(ships, Vector3.zero, HostileArmadaTracker.MIN_SHIPS);

            tracker.Update(ships, AddShip, assets, 0f);
            Assert.That(tracker.Armadas[0].ThreatensAsset, Is.False, "A fleet standing between two far assets threatens neither.");

            ships.Clear();
            float step = 2f * HostileArmadaTracker.HEADING_SAMPLE_SECONDS * 100f;
            AddCluster(ships, new Vector3(step, 0f, 0f), HostileArmadaTracker.MIN_SHIPS);
            tracker.Update(ships, AddShip, assets, 2f * HostileArmadaTracker.HEADING_SAMPLE_SECONDS);

            HostileArmada armada = tracker.Armadas[0];
            Assert.That(armada.Velocity.x, Is.GreaterThan(0f));
            Assert.That(armada.AssetKind, Is.EqualTo(ArmadaAssetKind.HeldZone));
            Assert.That(armada.AssetPosition, Is.EqualTo(east.Position));
        }

        private static void AddCluster(List<Vector3> ships, Vector3 centre, int count)
        {
            for (int i = 0; i < count; i++) ships.Add(centre + new Vector3(i * 50f, 0f, 0f));
        }

        private static void AddShip(int index, ForceComposition force) => force.Add(SHIP, 100f, 0f, 1f);

        private static UnitCombatProfile CreateProfile()
        {
            var damage = new float[ForceComposition.ClassSlots];
            damage[(int)ShipClass.Frigate] = 10f;
            return new UnitCombatProfile(ShipClass.Frigate, 100f, 0f, damage, damage, damage,
                Array.Empty<UnitCombatProfile>());
        }
    }
}
