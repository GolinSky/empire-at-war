using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.ShipSpawning;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>
    /// Round 6 (G1): hulls fly at per-type heights, so the 3D hull test lets two ship types land on one point. A
    /// forward landing keeps its top-down footprint until the ship has jumped in.
    /// </summary>
    [Category("Ships")]
    public sealed class ForwardLandingTests
    {
        private static readonly Vector3 UNIT = new Vector3(500f, 0f, 500f);

        [Test]
        public void SecondShipOfAnotherTypeDoesNotLandOnTheFirstShipsPoint()
        {
            var spawnPoints = new ShipSpawnPoints(null, null, new OpenRule(), null, null, new EntityLocator(),
                new ClearAtAnyHeight());
            var units = new List<Vector3> { UNIT };

            Assert.That(spawnPoints.TryGetSpawnPositionNearUnits(TestPlayers.Enemy, ShipType.Munificent, units,
                Vector3.zero, out Vector3 first, out _), Is.True);
            Assert.That(spawnPoints.TryGetSpawnPositionNearUnits(TestPlayers.Enemy, ShipType.Recusant, units,
                Vector3.zero, out Vector3 second, out _), Is.True);

            float planar = new Vector2(first.x - second.x, first.z - second.z).magnitude;
            Assert.That(planar, Is.GreaterThanOrEqualTo(2f * ClearAtAnyHeight.RADIUS));
        }

        /// <summary>Every point passes the 3D hull test, as two hulls at different heights do.</summary>
        private sealed class ClearAtAnyHeight : IShipSpawnClearance
        {
            public const float RADIUS = 60f;

            public float GetPlanarRadius(ShipType shipType) => RADIUS;

            public bool IsClear(PlayerId owner, ShipType shipType, Vector3 position) => true;

            public bool IsClear(ShipType shipType, Vector3 position, Quaternion rotation) => true;

            public void ReserveLanding(object ship, PlayerId owner, ShipType shipType, Vector3 position)
            {
            }

            public void ReserveLanding(object ship, ShipType shipType, Vector3 position, Quaternion rotation)
            {
            }

            public void ReleaseLanding(object ship)
            {
            }
        }

        private sealed class OpenRule : IReinforcementSpawnRule
        {
            public bool IsOpen(PlayerId team, Vector3 position) => true;

            public bool CanSpawnShip(PlayerId owner, ShipType shipType, Vector3 position) => true;

            public bool CanSpawnShip(PlayerId owner, ShipType shipType, Vector3 position, Quaternion rotation) => true;

            public bool CanLaunchStationDefenseShip(PlayerId owner, ShipType shipType, Vector3 position) => true;

            public bool IsOpenForStructure(PlayerId owner, Vector3 position) => true;

            public bool CanSpawnStructure(PlayerId owner, Vector3 position) => true;
        }
    }
}
