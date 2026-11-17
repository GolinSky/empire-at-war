using System.Collections.Generic;
using System.Diagnostics;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Players;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Globalization;
using System.IO;
using System.Text;
using EmpireAtWar.Services.Timing;
using UnityEngine;
using UnityEngine.SceneManagement;
#endif

namespace EmpireAtWar.Entities.Map
{
    /// <summary>
    /// Dev-only record of the generated battle map, so a reported battle can be regenerated from its seed.
    /// Writes persistentDataPath/BattleTelemetry/&lt;scene&gt;_&lt;time&gt;_map.csv once the layout is built.
    /// </summary>
    public static class BattleMapTelemetry
    {
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Write(BattleDefinition battle, IReadOnlyList<PlayerSlot> players, MapLayout layout)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // EditMode tests build maps too; only real battles are recorded.
            if (!Application.isPlaying) return;
            CultureInfo invariant = CultureInfo.InvariantCulture;
            BattlePerformanceCaptureMetadata metadata = BattlePerformanceCapture.CaptureMetadata();
            var text = new StringBuilder();
            text.AppendLine("section,key,value");
            AppendValue(text, "started_utc", DateTime.UtcNow.ToString("o", invariant));
            AppendValue(text, "source_revision", metadata.SourceRevision);
            AppendValue(text, "source_state", metadata.SourceState);
            AppendValue(text, "game_mode", battle.Mode.ToString());
            AppendValue(text, "map_type", battle.Map.Type.ToString());
            AppendValue(text, "map_size", battle.Map.Size.ToString());
            AppendValue(text, "planet", battle.Map.Planet.ToString());
            AppendValue(text, "map_seed", battle.Map.Seed.ToString(invariant));
            AppendValue(text, "victory_condition", battle.VictoryCondition.ToString());
            AppendValue(text, "features", battle.Features.ToString().Replace(',', '|'));
            AppendValue(text, "map_min", Format(layout.SizeRange.Min.x, layout.SizeRange.Min.y));
            AppendValue(text, "map_max", Format(layout.SizeRange.Max.x, layout.SizeRange.Max.y));
            AppendValue(text, "edge_margin", layout.EdgeMargin.ToString("F1", invariant));
            AppendValue(text, "planet_position", Format(layout.PlanetPosition.x, layout.PlanetPosition.z));
            AppendValue(text, "planet_obstacle_radius", layout.PlanetObstacleRadius.ToString("F1", invariant));
            AppendValue(text, "asteroid_fields", layout.Fields.Count.ToString(invariant));
            AppendValue(text, "decor_rocks", layout.DecorRocks.Count.ToString(invariant));
            AppendValue(text, "lanes", layout.Lanes.Count.ToString(invariant));

            text.AppendLine("section,id,owner,faction,team,controller,kind,x,z,detail");
            foreach (PlayerSlot player in players)
            {
                Vector3 station = layout.GetStationPosition(player.Id);
                text.AppendLine(string.Format(invariant, "player,{0},{0},{1},{2},{3},station,{4:F1},{5:F1},",
                    player.Id, player.Faction, player.Team, player.Controller, station.x, station.z));
            }

            for (int i = 0; i < layout.Zones.Count; i++)
            {
                ZoneSpot zone = layout.Zones[i];
                text.AppendLine(string.Format(invariant, "zone,{0},{1},,,,{2},{3:F1},{4:F1},",
                    i, zone.Owner, zone.IsMain ? "main" : zone.IsCapturable ? "capturable" : "fixed",
                    zone.Center.x, zone.Center.z));
            }

            for (int i = 0; i < layout.Sites.Count; i++)
            {
                SiteSpot site = layout.Sites[i];
                text.AppendLine(string.Format(invariant, "site,{0},{1},,,,{2},{3:F1},{4:F1},point={5}",
                    i, site.Owner, site.Kind, site.Center.x, site.Center.z, site.PointId));
            }

            for (int i = 0; i < layout.Fields.Count; i++)
            {
                AsteroidField field = layout.Fields[i];
                text.AppendLine(string.Format(invariant, "field,{0},,,,,asteroids,,,rocks={1};floor={2:F1};ceiling={3:F1}",
                    i, field.Rocks.Count, field.Floor, field.Ceiling));
            }

            for (int i = 0; i < layout.Hazards.Count; i++)
            {
                Bounds bounds = layout.Hazards[i].Bounds;
                text.AppendLine(string.Format(invariant, "hazard,{0},,,,,bounds,{1:F1},{2:F1},size={3:F1}x{4:F1}",
                    i, bounds.center.x, bounds.center.z, bounds.size.x, bounds.size.z));
            }

            string path = Path.Combine(Application.persistentDataPath, "BattleTelemetry",
                $"{SceneManager.GetActiveScene().name}_{DateTime.Now:yyyyMMdd_HHmmss}_map.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, text.ToString());
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static void AppendValue(StringBuilder text, string key, string value) =>
            text.Append("map,").Append(key).Append(',').AppendLine(value);

        private static string Format(float x, float z) =>
            string.Format(CultureInfo.InvariantCulture, "{0:F1};{1:F1}", x, z);
#endif
    }
}
