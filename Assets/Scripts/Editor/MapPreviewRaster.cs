using System.Collections.Generic;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Generation;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Editor
{
    /// <summary>Top-down schematic of actual field volumes, hazard masks, lanes and capture footprints.</summary>
    public sealed class MapPreviewRaster
    {
        public const int RESOLUTION = 512;
        private const int BORDER = 12;
        private readonly MapLayout _layout;
        private readonly Color32[] _pixels = new Color32[RESOLUTION * RESOLUTION];
        private readonly float _scale;

        public MapPreviewRaster(MapLayout layout)
        {
            _layout = layout;
            _scale = (RESOLUTION - BORDER * 2f) /
                Mathf.Max(layout.SizeRange.Max.x - layout.SizeRange.Min.x, layout.SizeRange.Max.y - layout.SizeRange.Min.y);
        }

        public Texture2D Render(IReadOnlyList<PlayerSlot> players, MapGenerationSettings settings, MapFeatureRadii radii)
        {
            for (int y = 0; y < RESOLUTION; y++)
            for (int x = 0; x < RESOLUTION; x++)
                _pixels[y * RESOLUTION + x] = x % 32 == 0 || y % 32 == 0
                    ? new Color32(19, 29, 42, 255) : new Color32(11, 19, 30, 255);
            foreach (AsteroidField field in _layout.Fields)
            foreach (FieldVolume volume in field.Volumes)
                Circle(ToPixel(volume.Center), volume.Radius * _scale, new Color32(76, 87, 100, 255));
            foreach (HazardShape hazard in _layout.Hazards) DrawHazard(hazard);
            foreach (MapLane lane in _layout.Lanes)
                for (int i = 1; i < lane.Points.Count; i++)
                    Line(ToPixel(lane.Points[i - 1]), ToPixel(lane.Points[i]), new Color32(58, 122, 153, 255));
            foreach (ZoneSpot zone in _layout.Zones)
            {
                float radius = (zone.IsMain ? radii.MainZone : radii.Zone) * _scale;
                Color32 color = zone.IsMain ? new Color32(255, 196, 80, 255) :
                    zone.IsCapturable ? new Color32(214, 225, 235, 255) : new Color32(102, 170, 196, 255);
                Ring(ToPixel(zone.Center), radius, color);
                Circle(ToPixel(zone.Center), zone.IsMain ? 4f : 2f, color);
            }
            foreach (SiteSpot site in _layout.Sites)
            {
                Color32 color = site.Kind == CaptureSiteKind.Mining
                    ? new Color32(248, 186, 92, 255) : new Color32(165, 213, 206, 255);
                Circle(ToPixel(site.Center), site.Kind == CaptureSiteKind.Mining ? 3f : 2f, color);
            }
            foreach (PlayerSlot player in players)
            {
                Vector2 point = ToPixel(_layout.GetStationPosition(player.Id));
                Color32 color = player.Team.Value == 0 ? new Color32(83, 217, 255, 255) : new Color32(255, 106, 115, 255);
                Ring(point, settings.GetStationRadius(player.Faction) * _scale, color);
                Circle(point, 5f, color);
            }
            Texture2D texture = new Texture2D(RESOLUTION, RESOLUTION, TextureFormat.RGBA32, false);
            texture.SetPixels32(_pixels);
            texture.Apply(false);
            return texture;
        }

        private void DrawHazard(HazardShape hazard)
        {
            Vector2 min = ToPixel(hazard.Bounds.min);
            Vector2 max = ToPixel(hazard.Bounds.max);
            Color32 color = hazard.Data.HazardType == HazardType.IonStorm
                ? new Color32(104, 72, 155, 255) : new Color32(47, 109, 107, 255);
            for (int y = Mathf.Max(0, Mathf.FloorToInt(min.y)); y <= Mathf.Min(RESOLUTION - 1, Mathf.CeilToInt(max.y)); y++)
            for (int x = Mathf.Max(0, Mathf.FloorToInt(min.x)); x <= Mathf.Min(RESOLUTION - 1, Mathf.CeilToInt(max.x)); x++)
            {
                Vector3 world = new Vector3(_layout.SizeRange.Min.x + (x - BORDER) / _scale, 0f,
                    _layout.SizeRange.Min.y + (y - BORDER) / _scale);
                if (hazard.Contains(world, 0f, false)) _pixels[y * RESOLUTION + x] = color;
            }
        }

        private Vector2 ToPixel(Vector3 world) => new Vector2(BORDER + (world.x - _layout.SizeRange.Min.x) * _scale,
            BORDER + (world.z - _layout.SizeRange.Min.y) * _scale);

        private void Line(Vector2 start, Vector2 end, Color32 color)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(start, end));
            for (int i = 0; i <= steps; i++)
                Circle(Vector2.Lerp(start, end, steps == 0 ? 0f : (float)i / steps), 0.8f, color);
        }

        private void Ring(Vector2 center, float radius, Color32 color)
        {
            int steps = Mathf.CeilToInt(radius * Mathf.PI * 2f);
            for (int i = 0; i < steps; i++)
            {
                float angle = i * Mathf.PI * 2f / steps;
                Circle(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, 1f, color);
            }
        }

        private void Circle(Vector2 center, float radius, Color32 color)
        {
            for (int y = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
                 y <= Mathf.Min(RESOLUTION - 1, Mathf.CeilToInt(center.y + radius)); y++)
            for (int x = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
                 x <= Mathf.Min(RESOLUTION - 1, Mathf.CeilToInt(center.x + radius)); x++)
                if ((new Vector2(x, y) - center).sqrMagnitude <= radius * radius)
                    _pixels[y * RESOLUTION + x] = color;
        }
    }
}
