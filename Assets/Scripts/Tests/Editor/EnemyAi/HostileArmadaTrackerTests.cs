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

        private const int CLUSTER_SHIPS = 6;

        [Test]
        public void Update_EachDistantClusterIsItsOwnGroup()
        {
            var ships = new List<Vector3>();
            AddCluster(ships, Vector3.zero, CLUSTER_SHIPS);
            AddCluster(ships, new Vector3(5000f, 0f, 0f), CLUSTER_SHIPS);
            AddCluster(ships, new Vector3(0f, 0f, 5000f), 1);
            var tracker = new HostileArmadaTracker();

            tracker.Update(ships, AddShip, Array.Empty<ArmadaAsset>(), 0f);

            Assert.That(tracker.Armadas.Count, Is.EqualTo(3));
            foreach (HostileArmada armada in tracker.Armadas)
                Assert.That(armada.Strength.UnitCount, Is.EqualTo(armada.Ships));
        }

        [Test]
        public void IsArmada_TwoCapitalsAlwaysCount_SmallShipsTheFleetBeatsTwoToOneDoNot()
        {
            var capitals = new HostileArmada();
            capitals.Strength.Add(CreateProfile(ShipClass.Capital), 100f, 0f, 1f);
            capitals.Strength.Add(CreateProfile(ShipClass.Capital), 100f, 0f, 1f);
            var corvettes = new HostileArmada();
            for (int i = 0; i < CLUSTER_SHIPS; i++) corvettes.Strength.Add(SHIP, 100f, 0f, 1f);

            Assert.That(HostileArmadaTracker.IsArmada(capitals, HostileArmadaTracker.OUTMATCH_RATIO * 10f), Is.True);
            Assert.That(HostileArmadaTracker.IsArmada(corvettes, HostileArmadaTracker.OUTMATCH_RATIO * 2f), Is.False);
            Assert.That(HostileArmadaTracker.IsArmada(corvettes, HostileArmadaTracker.OUTMATCH_RATIO * 0.5f), Is.True,
                "A group the fleet does not beat 2 : 1 is an armada whatever its size.");
            var lone = new HostileArmada();
            lone.Strength.Add(CreateProfile(ShipClass.Capital), 100f, 0f, 1f);
            Assert.That(HostileArmadaTracker.IsArmada(lone, HostileArmadaTracker.OUTMATCH_RATIO * 0.5f), Is.False,
                "A lone zone guard never switches on the late-game rules.");
        }

        [Test]
        public void Update_MovingArmadaThreatensTheAssetItHeadsFor()
        {
            var west = new ArmadaAsset(new Vector3(-2500f, 0f, 0f), 0f, ArmadaAssetKind.Station);
            var east = new ArmadaAsset(new Vector3(2500f, 0f, 0f), 0f, ArmadaAssetKind.HeldZone);
            ArmadaAsset[] assets = { west, east };
            var tracker = new HostileArmadaTracker();
            var ships = new List<Vector3>();
            AddCluster(ships, Vector3.zero, CLUSTER_SHIPS);

            tracker.Update(ships, AddShip, assets, 0f);
            Assert.That(tracker.Armadas[0].ThreatensAsset, Is.False, "A fleet standing between two far assets threatens neither.");

            ships.Clear();
            float step = 2f * HostileArmadaTracker.HEADING_SAMPLE_SECONDS * 100f;
            AddCluster(ships, new Vector3(step, 0f, 0f), CLUSTER_SHIPS);
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

        private static UnitCombatProfile CreateProfile(ShipClass shipClass = ShipClass.Frigate)
        {
            var damage = new float[ForceComposition.ClassSlots];
            damage[(int)ShipClass.Frigate] = 10f;
            return new UnitCombatProfile(shipClass, 100f, 0f, damage, damage, damage,
                Array.Empty<UnitCombatProfile>());
        }
    }
}
