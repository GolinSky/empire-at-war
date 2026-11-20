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

        /// <summary>Heading from <paramref name="position"/> toward the owner's designated enemy spawn.</summary>
        Quaternion GetEnemyFacing(PlayerId owner, Vector3 position);
    }

    /// <summary>Every station, and the units it launches, faces the map center where the players meet.
    /// Reinforcements face one designated enemy spawn: the hostile station nearest to the owner's own.</summary>
    public sealed class StationFacingService : IStationFacingService
    {
        private readonly Dictionary<PlayerId, Quaternion> _rotations = new Dictionary<PlayerId, Quaternion>();
        private readonly Dictionary<PlayerId, Vector3> _enemySpawns = new Dictionary<PlayerId, Vector3>();
        private readonly IMapModelObserver _mapModel;
        private readonly IPlayerRoster _roster;

        // The map loads after binding, so each rotation is derived on its first request.
        public StationFacingService(IMapModelObserver mapModel, IPlayerRoster roster)
        {
            _mapModel = mapModel;
            _roster = roster;
        }

        public Quaternion GetEnemyFacing(PlayerId owner, Vector3 position)
        {
            // A spawn chosen once never switches, even after that station falls.
            if (!_enemySpawns.TryGetValue(owner, out Vector3 enemySpawn))
            {
                if (!TryFindNearestEnemySpawn(owner, out enemySpawn))
                {
                    return GetRotation(owner);
                }

                _enemySpawns.Add(owner, enemySpawn);
            }

            Vector3 toEnemy = enemySpawn - position;
            toEnemy.y = 0f;
            return toEnemy.sqrMagnitude <= Mathf.Epsilon
                ? GetRotation(owner)
                : Quaternion.LookRotation(toEnemy, Vector3.up);
        }

        private bool TryFindNearestEnemySpawn(PlayerId owner, out Vector3 enemySpawn)
        {
            Vector3 ownSpawn = _mapModel.GetStationPosition(owner);
            float bestDistance = float.MaxValue;
            enemySpawn = default;
            foreach (PlayerSlot player in _roster.Players)
            {
                if (!_roster.IsHostile(owner, player.Id)) continue;
                Vector3 spawn = _mapModel.GetStationPosition(player.Id);
                float distance = (spawn - ownSpawn).sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                enemySpawn = spawn;
            }

            return bestDistance < float.MaxValue;
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
