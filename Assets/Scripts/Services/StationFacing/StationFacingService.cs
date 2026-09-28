using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.StationFacing
{
    public interface IStationFacingService
    {
        Quaternion GetRotation(PlayerId owner);
    }

    /// <summary>Every station, and the units it launches, faces the map center where the players meet.</summary>
    public sealed class StationFacingService : IStationFacingService
    {
        private readonly Dictionary<PlayerId, Quaternion> _rotations = new Dictionary<PlayerId, Quaternion>();

        public StationFacingService(IMapModelObserver mapModel, IPlayerRoster roster)
        {
            Vector3 center = new Vector3(
                (mapModel.SizeRange.Min.x + mapModel.SizeRange.Max.x) * 0.5f,
                0f,
                (mapModel.SizeRange.Min.y + mapModel.SizeRange.Max.y) * 0.5f);
            foreach (PlayerSlot player in roster.Players)
            {
                Vector3 toCenter = center - mapModel.GetStationPosition(player.Id);
                toCenter.y = 0f;
                if (toCenter.sqrMagnitude <= Mathf.Epsilon)
                {
                    throw new InvalidOperationException($"The station of {player.Id} sits on the map center.");
                }

                _rotations.Add(player.Id, Quaternion.LookRotation(toCenter, Vector3.up));
            }
        }

        public Quaternion GetRotation(PlayerId owner)
        {
            return _rotations[owner];
        }
    }
}
