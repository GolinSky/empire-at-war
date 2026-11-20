using System.Collections.Generic;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.StationFacing;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Navigation")]
    public sealed class StationFacingServiceTests
    {
        [Test]
        public void GetRotation_FacesEveryStationTowardMapCenterOnce()
        {
            // The default size range is centered on the origin.
            CountingMapModel mapModel = new CountingMapModel(new Dictionary<PlayerId, Vector3>
            {
                { TestPlayers.Human, new Vector3(-10f, 3f, 0f) },
                { TestPlayers.Enemy, new Vector3(20f, 9f, 0f) },
            });
            StationFacingService service = new StationFacingService(mapModel, TestPlayers.CreateDuel());

            Quaternion playerRotation = service.GetRotation(TestPlayers.Human);
            Quaternion opponentRotation = service.GetRotation(TestPlayers.Enemy);
            service.GetRotation(TestPlayers.Human);

            Assert.That(mapModel.PositionRequestCount, Is.EqualTo(2));
            Assert.That(
                Quaternion.Angle(playerRotation, Quaternion.LookRotation(Vector3.right)),
                Is.LessThan(0.001f));
            Assert.That(
                Quaternion.Angle(opponentRotation, Quaternion.LookRotation(Vector3.left)),
                Is.LessThan(0.001f));
        }

        [Test]
        public void GetEnemyFacing_TurnsTowardTheNearestHostileSpawnAndKeepsIt()
        {
            // The ally sits closest, so it must be skipped; Enemy is the nearest hostile spawn.
            CountingMapModel mapModel = new CountingMapModel(new Dictionary<PlayerId, Vector3>
            {
                { TestPlayers.Human, Vector3.zero },
                { TestPlayers.Ally, new Vector3(5f, 0f, 0f) },
                { TestPlayers.Enemy, new Vector3(0f, 0f, 50f) },
                { TestPlayers.SecondEnemy, new Vector3(-80f, 0f, 0f) },
            });
            StationFacingService service = new StationFacingService(mapModel, TestPlayers.CreateTeamGame());

            Quaternion fromOrigin = service.GetEnemyFacing(TestPlayers.Human, Vector3.zero);
            int requestsAfterChoice = mapModel.PositionRequestCount;
            // Standing much closer to the second enemy must not switch the target.
            Quaternion fromWest = service.GetEnemyFacing(TestPlayers.Human, new Vector3(-60f, 4f, 50f));

            Assert.That(Quaternion.Angle(fromOrigin, Quaternion.LookRotation(Vector3.forward)), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(fromWest, Quaternion.LookRotation(Vector3.right)), Is.LessThan(0.001f));
            Assert.That(mapModel.PositionRequestCount, Is.EqualTo(requestsAfterChoice));
        }

        private sealed class CountingMapModel : IMapModelObserver
        {
            private readonly Dictionary<PlayerId, Vector3> _positions;

            public int PositionRequestCount { get; private set; }
            public Vector2Range SizeRange { get; } = new Vector2Range();

            public CountingMapModel(Dictionary<PlayerId, Vector3> positions)
            {
                _positions = positions;
            }

            public Vector3 GetStationPosition(PlayerId owner)
            {
                PositionRequestCount++;
                return _positions[owner];
            }
        }
    }
}
