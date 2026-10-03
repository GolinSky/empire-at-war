using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.StationFacing;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class StationFacingServiceTests
    {
        [Test]
        public void Constructor_FacesEveryStationTowardMapCenterOnce()
        {
            // The default size range is centered on the origin.
            CountingMapModel mapModel = new CountingMapModel(
                new Vector3(-10f, 3f, 0f),
                new Vector3(20f, 9f, 0f));
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

        private sealed class CountingMapModel : IMapModelObserver
        {
            private readonly Vector3 _humanPosition;
            private readonly Vector3 _enemyPosition;

            public int PositionRequestCount { get; private set; }
            public Vector2Range SizeRange { get; } = new Vector2Range();

            public CountingMapModel(Vector3 humanPosition, Vector3 enemyPosition)
            {
                _humanPosition = humanPosition;
                _enemyPosition = enemyPosition;
            }

            public Vector3 GetStationPosition(PlayerId owner)
            {
                PositionRequestCount++;
                return owner == TestPlayers.Human ? _humanPosition : _enemyPosition;
            }
        }
    }
}
