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
            float mineSpread = (2f * _radii.MiningSite + 15f) *
                PointOfInterestPlacer.MINING_SITE_SPACING_SHARE / Mathf.Sqrt(3f);
            float satelliteSpread = mineSpread + _settings.SatellitePadOffset;
            float pointExtent = Mathf.Max(mineSpread + _radii.MiningSite, satelliteSpread + _settings.SatelliteFootprintRadius);
            float miningRadius = Mathf.Max(radius + _radii.MiningSite + size.HomeMiningGap.Min,
                radius + 2f * _radii.Zone + size.DefaultZoneGap + pointExtent + _settings.ZoneClearance);
            miningRadius = Mathf.Max(miningRadius,
                (pointExtent + _settings.ZoneClearance) / Mathf.Sin(Mathf.PI / size.SurvivalMiningSiteCount));
            if (miningRadius + pointExtent > Mathf.Min(size.Bounds.Max.x - middle.x, size.Bounds.Max.y - middle.y))
                throw new InvalidOperationException("Survival mining points and satellite pads do not fit inside the map.");

            for (int i = 0; i < size.SurvivalMiningSiteCount; i++)
            {
                float turn = i * 360f / size.SurvivalMiningSiteCount;
                Vector3 direction = Quaternion.Euler(0f, turn, 0f) * Vector3.forward;
                Vector3 point = new Vector3(middle.x, 0f, middle.y) + direction * miningRadius;
                for (int site = 0; site < PointOfInterestPlacer.SITES_PER_MINING_POINT; site++)
                {
                    Vector3 mineOffset = Quaternion.Euler(0f, site * 120f, 0f) * direction * mineSpread;
                    Vector3 padOffset = Quaternion.Euler(0f, site * 120f + 60f, 0f) * direction * satelliteSpread;
                    sites.Add(new SiteSpot(point + mineOffset, CaptureSiteKind.Mining, PlayerId.None, i));
                    sites.Add(new SiteSpot(point + padOffset, CaptureSiteKind.Satellite, battle.Human.Slot.Id, i));
                }
            }
            return new MapLayout(stations, zones, sites, Array.Empty<MapLane>(), Array.Empty<AsteroidField>(),
                size.Bounds, new Vector3(middle.x, 0, middle.y));
        }
    }
}
