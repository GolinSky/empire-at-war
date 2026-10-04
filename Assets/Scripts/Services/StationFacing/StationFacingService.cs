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
        private readonly IMapModelObserver _mapModel;

        // The map loads after binding, so each rotation is derived on its first request.
        public StationFacingService(IMapModelObserver mapModel)
        {
            _mapModel = mapModel;
        }

        public Quaternion GetRotation(PlayerId owner)
        {
            if (_rotations.TryGetValue(owner, out Quaternion rotation))
            {
                return rotation;
            }

            Vector3 center = new Vector3(
                (_mapModel.SizeRange.Min.x + _mapModel.SizeRange.Max.x) * 0.5f,
                0f,
                (_mapModel.SizeRange.Min.y + _mapModel.SizeRange.Max.y) * 0.5f);
            Vector3 toCenter = center - _mapModel.GetStationPosition(owner);
            toCenter.y = 0f;
            if (toCenter.sqrMagnitude <= Mathf.Epsilon)
            {
                throw new InvalidOperationException($"The station of {owner} sits on the map center.");
            }

            rotation = Quaternion.LookRotation(toCenter, Vector3.up);
            _rotations.Add(owner, rotation);
            return rotation;
        }
    }
}
