using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Entities.Map.Generation
{
    public sealed class SurvivalLayoutGenerator
    {
        private readonly MapGenerationSettings _settings;
        private readonly MapFeatureRadii _radii;
        public SurvivalLayoutGenerator(MapGenerationSettings settings, MapFeatureRadii radii)
        {
            _settings = settings;
            _radii = radii;
        }

        public MapLayout Generate(BattleDefinition battle)
        {
            MapSizeSettings size = _settings.GetSize(battle.Map.Size);
            Vector2 middle = (size.Bounds.Min + size.Bounds.Max) * 0.5f;
            Vector3 center = new Vector3(middle.x, _settings.StationHeight, middle.y);
            Dictionary<PlayerId, Vector3> stations = new Dictionary<PlayerId, Vector3>();
            List<ZoneSpot> zones = new List<ZoneSpot>();
            List<SiteSpot> sites = new List<SiteSpot>();
            float radius = _settings.GetStationRadius(battle.Survival.PlayerFaction);
            foreach (BattlePlayer player in battle.Players)
            {
                stations.Add(player.Slot.Id, center);
                if (player.CanProduce)
                    zones.Add(new ZoneSpot(center + Vector3.forward * (radius + _radii.Zone + size.DefaultZoneGap),
                        player.Slot.Id, false));
            }
            float miningRadius = radius + _radii.MiningSite + size.HomeMiningGap.Min;
            for (int i = 0; i < size.MiningPointCount; i++)
            {
                float angle = i * Mathf.PI * 2 / size.MiningPointCount;
                sites.Add(new SiteSpot(new Vector3(middle.x + Mathf.Cos(angle) * miningRadius, 0,
                    middle.y + Mathf.Sin(angle) * miningRadius), SiteFacilityType.Mining));
            }
            return new MapLayout(stations, zones, sites, Array.Empty<MapLane>(), Array.Empty<AsteroidField>(),
                size.Bounds, new Vector3(middle.x, 0, middle.y));
        }
    }
}
