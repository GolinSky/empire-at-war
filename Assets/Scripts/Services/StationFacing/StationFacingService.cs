using System;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Services.StationFacing
{
    public interface IStationFacingService
    {
        Quaternion GetRotation(PlayerType playerType);
    }

    public sealed class StationFacingService : IStationFacingService
    {
        private readonly Quaternion _playerRotation;
        private readonly Quaternion _opponentRotation;

        public StationFacingService(IMapModelObserver mapModel)
        {
            if (mapModel == null)
            {
                throw new ArgumentNullException(nameof(mapModel));
            }

            Vector3 playerToOpponent =
                mapModel.GetStationPosition(PlayerType.Opponent) -
                mapModel.GetStationPosition(PlayerType.Player);
            playerToOpponent.y = 0f;
            if (playerToOpponent.sqrMagnitude <= Mathf.Epsilon)
            {
                throw new InvalidOperationException(
                    "Player and opponent stations must have different horizontal positions.");
            }

            _playerRotation = Quaternion.LookRotation(playerToOpponent, Vector3.up);
            _opponentRotation = Quaternion.LookRotation(-playerToOpponent, Vector3.up);
        }

        public Quaternion GetRotation(PlayerType playerType)
        {
            return playerType switch
            {
                PlayerType.Player => _playerRotation,
                PlayerType.Opponent => _opponentRotation,
                _ => throw new ArgumentOutOfRangeException(nameof(playerType), playerType, null)
            };
        }
    }
}
