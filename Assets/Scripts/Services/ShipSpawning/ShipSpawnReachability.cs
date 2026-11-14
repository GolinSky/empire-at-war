using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.ShipNavigation;
using EmpireAtWar.Services.Timing;
using UnityEngine;

namespace EmpireAtWar.Services.ShipSpawning
{
    public sealed class ShipSpawnReachability : IShipSpawnReachability
    {
        private readonly IAssetService _assetService;
        private readonly IShipNavigationService _navigation;
        private readonly IMapModelObserver _mapModel;
        private readonly ShipsData _shipsData;
        private readonly Dictionary<ShipType, Profile> _profiles = new Dictionary<ShipType, Profile>();

        public ShipSpawnReachability(
            IAssetService assetService,
            IShipNavigationService navigation,
            IMapModelObserver mapModel,
            ShipsData shipsData)
        {
            _assetService = assetService;
            _navigation = navigation;
            _mapModel = mapModel;
            _shipsData = shipsData;
        }

        public bool IsReachable(PlayerId owner, ShipType shipType, Vector3 position)
        {
            Profile profile = GetProfile(shipType);
            if (profile.IgnoresMapObstacles) return true;
            return _navigation.IsReachable(_mapModel.GetStationPosition(owner), position,
                profile.NavigationRadius, profile.Span, _mapModel.SizeRange);
        }

        public void FillReachability(PlayerId owner, ShipType shipType, Vector3[] positions, bool[] reachable)
        {
            using var marker = BattleProfilerMarkers.SpawnAreaReachability.Auto();
            Profile profile = GetProfile(shipType);
            if (profile.IgnoresMapObstacles)
            {
                Array.Fill(reachable, true);
                return;
            }
            _navigation.FillReachability(_mapModel.GetStationPosition(owner), positions, reachable,
                profile.NavigationRadius, profile.Span, _mapModel.SizeRange);
        }

        public bool TryFillReachability(PlayerId owner, ShipType shipType, Vector3[] positions, bool[] reachable,
            bool forceRefresh)
        {
            using var marker = BattleProfilerMarkers.SpawnAreaReachability.Auto();
            Profile profile = GetProfile(shipType);
            if (profile.IgnoresMapObstacles)
            {
                Array.Fill(reachable, true);
                return true;
            }
            return _navigation.TryFillReachability(_mapModel.GetStationPosition(owner), positions, reachable,
                profile.NavigationRadius, profile.Span, _mapModel.SizeRange, forceRefresh);
        }

        private Profile GetProfile(ShipType shipType)
        {
            if (_profiles.TryGetValue(shipType, out Profile profile)) return profile;
            ShipData shipData = _assetService.Load<ShipData>(_shipsData.GetShipDataPath(shipType));
            profile = new Profile(shipData);
            _profiles.Add(shipType, profile);
            return profile;
        }

        private readonly struct Profile
        {
            public Profile(ShipData shipData)
            {
                // Same clearance and span ShipMoveComponent plans with, so the check shares its navigation mesh.
                NavigationRadius = Mathf.Max(shipData.NavigationRadius, ShipMoveComponent.MINIMUM_NAVIGATION_RADIUS);
                Span = new ShipHullSpan(shipData.Height + shipData.HullBottom, shipData.Height + shipData.HullTop);
                IgnoresMapObstacles = shipData.IgnoresMapObstacles;
            }

            public float NavigationRadius { get; }
            public ShipHullSpan Span { get; }
            public bool IgnoresMapObstacles { get; }
        }
    }
}
