using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.StationFacing;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class StationFacingServiceTests
    {
        [Test]
        public void Constructor_CalculatesBothRotationsOnceAndReusesThem()
        {
            CountingMapModel mapModel = new CountingMapModel(
                new Vector3(-10f, 3f, 5f),
                new Vector3(20f, 9f, 5f));
            StationFacingService service = new StationFacingService(mapModel);

            Quaternion playerRotation = service.GetRotation(PlayerType.Player);
            Quaternion opponentRotation = service.GetRotation(PlayerType.Opponent);
            service.GetRotation(PlayerType.Player);

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
            private readonly Vector3 _playerPosition;
            private readonly Vector3 _opponentPosition;

            public CountingMapModel(Vector3 playerPosition, Vector3 opponentPosition)
            {
                _playerPosition = playerPosition;
                _opponentPosition = opponentPosition;
            }

            public int PositionRequestCount { get; private set; }
            public Vector2Range SizeRange => default;

            public Vector3 GetStationPosition(PlayerType playerType)
            {
                PositionRequestCount++;
                return playerType == PlayerType.Player
                    ? _playerPosition
                    : _opponentPosition;
            }
        }
    }
}
