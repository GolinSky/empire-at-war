using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Entities.Map.Generation
{
    public sealed class SurvivalLayoutGenerator
    {
        private const int SATELLITE_PADS_PER_POINT = 3;

        private readonly MapGenerationSettings _settings;
        private readonly MapFeatureRadii _radii;
        public SurvivalLayoutGenerator(MapGenerationSettings settings, MapFeatureRadii radii)
        {
            _settings = settings;
            _radii = radii;
        }

        internal static int GetSiteCount(MapSizeSettings size, bool useLegacyMiningLayout) =>
            size.SurvivalMiningSiteCount * (SATELLITE_PADS_PER_POINT +
                (useLegacyMiningLayout ? SATELLITE_PADS_PER_POINT : 1));

        public MapLayout Generate(BattleDefinition battle, bool useLegacyMiningLayout = false)
        {
            MapSizeSettings size = _settings.GetSize(battle.Map.Size);
            Vector2 middle = (size.Bounds.Min + size.Bounds.Max) * 0.5f;
            Vector3 center = new Vector3(middle.x, _settings.StationHeight, middle.y);
            Dictionary<PlayerId, Vector3> stations = new Dictionary<PlayerId, Vector3>();
            List<ZoneSpot> zones = new List<ZoneSpot>();
            List<SiteSpot> sites = new List<SiteSpot>();
            IReadOnlyList<BattlePlayer> humans = battle.Humans;
            float radius = humans.Max(human => _settings.GetStationRadius(human.Slot.Faction));
            // One human keeps the station at the center; co-op humans share a ring around it.
            float ring = humans.Count > 1
                ? (radius + 0.5f * size.DefaultZoneGap) / Mathf.Sin(Mathf.PI / humans.Count)
                : 0f;
            Dictionary<PlayerId, Vector3> directions = new Dictionary<PlayerId, Vector3>();
            for (int i = 0; i < humans.Count; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, i * 360f / humans.Count, 0f) * Vector3.forward;
                directions.Add(humans[i].Slot.Id, direction);
                stations.Add(humans[i].Slot.Id, center + direction * ring);
            }
            foreach (BattlePlayer player in battle.Players)
            {
                if (player.Role == PlayerRole.Human) continue;
                // Hosted allies share their host's station; the wave attacker is homed on the center.
                PlayerId home = player.IsHosted ? player.ProductionHostId : player.Slot.Id;
                stations.Add(player.Slot.Id, stations.TryGetValue(home, out Vector3 hosted) ? hosted : center);
                if (!directions.ContainsKey(player.Slot.Id))
                    directions.Add(player.Slot.Id, directions.TryGetValue(home, out Vector3 hostDirection)
                        ? hostDirection : Vector3.forward);
            }
            foreach (BattlePlayer player in battle.Players)
            {
                if (player.CanProduce)
                    zones.Add(new ZoneSpot(stations[player.Slot.Id] +
                        directions[player.Slot.Id] * (radius + _radii.Zone + size.DefaultZoneGap), player.Slot.Id, false));
            }
            float stationExtent = radius + ring;
            float mineSpread = (2f * _radii.MiningSite + 15f) *
                PointOfInterestPlacer.MINING_SITE_SPACING_SHARE / Mathf.Sqrt(3f);
            float satelliteSpread = mineSpread + _settings.SatellitePadOffset;
            float pointExtent = Mathf.Max(mineSpread + _radii.MiningSite, satelliteSpread + _settings.SatelliteFootprintRadius);
            float miningRadius = Mathf.Max(stationExtent + _radii.MiningSite + size.HomeMiningGap.Min,
                stationExtent + 2f * _radii.Zone + size.DefaultZoneGap + pointExtent + _settings.ZoneClearance);
            miningRadius = Mathf.Max(miningRadius,
                (pointExtent + _settings.ZoneClearance) / Mathf.Sin(Mathf.PI / size.SurvivalMiningSiteCount));
            if (miningRadius + pointExtent > Mathf.Min(size.Bounds.Max.x - middle.x, size.Bounds.Max.y - middle.y))
                throw new InvalidOperationException("Survival mining points and satellite pads do not fit inside the map.");

            for (int i = 0; i < size.SurvivalMiningSiteCount; i++)
            {
                float turn = i * 360f / size.SurvivalMiningSiteCount;
                Vector3 direction = Quaternion.Euler(0f, turn, 0f) * Vector3.forward;
                Vector3 point = new Vector3(middle.x, 0f, middle.y) + direction * miningRadius;
                if (!useLegacyMiningLayout)
                    sites.Add(new SiteSpot(point, CaptureSiteKind.Mining, PlayerId.None, i));
                // Older saves restore capture sites by index, so keep their original positions and order.
                for (int site = 0; site < SATELLITE_PADS_PER_POINT; site++)
                {
                    if (useLegacyMiningLayout)
                    {
                        Vector3 mineOffset = Quaternion.Euler(0f, site * 120f, 0f) * direction * mineSpread;
                        sites.Add(new SiteSpot(point + mineOffset, CaptureSiteKind.Mining, PlayerId.None, i));
                    }
                    Vector3 padOffset = Quaternion.Euler(0f, site * 120f + 60f, 0f) * direction * satelliteSpread;
                    // Satellite pads start owned by the defenders, shared out between co-op humans.
                    sites.Add(new SiteSpot(point + padOffset, CaptureSiteKind.Satellite, humans[i % humans.Count].Slot.Id, i));
                }
            }
            return new MapLayout(stations, zones, sites, Array.Empty<MapLane>(), Array.Empty<AsteroidField>(),
                size.Bounds, new Vector3(middle.x, 0, middle.y));
        }
    }
}
