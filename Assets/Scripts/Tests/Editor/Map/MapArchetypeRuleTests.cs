using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Generation;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Utils;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Map")]
    public sealed class MapArchetypeRuleTests
    {
        private const string SETTINGS_PATH = "Assets/Settings/Data/Models/Map/MapGenerationSettings.asset";

        [TestCase(-1f)]
        [TestCase(0f)]
        [TestCase(1f)]
        public void BeltPasses_RemainOpenEvenAtMaximumNoise(float pass)
        {
            MapSizeSettings size = GetSize(PlanetType.Hoth);
            Vector3 center = PlanarGeometry.GetCenter(size.Bounds);
            float half = (size.Bounds.Max.x - size.Bounds.Min.x) * 0.5f;
            Quaternion turn = Quaternion.Euler(0f, size.SkeletonAngle, 0f);
            float gap = size.MinLaneWidth * 0.5f;
            foreach (float across in new[] { -gap, 0f, gap })
            foreach (float along in new[] { -0.2f * half, 0f, 0.2f * half })
            {
                Vector3 point = center + turn * new Vector3(pass * half * 0.62f + across, 0f, along);
                Assert.That(ArchetypeFieldPattern.IsField(point, size, 1f, 1f), Is.False,
                    $"Pass {pass}, across {across}, along {along}");
            }
        }

        [Test]
        public void BeltGap_TakesPriorityOverRelayPocket()
        {
            MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            MapSizeSettings size = GetSize(PlanetType.Hoth);
            Vector3 center = PlanarGeometry.GetCenter(size.Bounds);
            float half = (size.Bounds.Max.x - size.Bounds.Min.x) * 0.5f;
            Quaternion turn = Quaternion.Euler(0f, size.SkeletonAngle, 0f);
            Vector3 relay = center + turn * new Vector3(half * 0.62f, 0f, 0f);
            Vector3 sample = relay + turn * Vector3.forward *
                (270f + settings.ZoneClearance + size.PocketThickness * 0.75f);
            FieldDensity density = new FieldDensity(System.Array.Empty<MapStation>(),
                new[] { new MapNode(MapNodeKind.CapturableZone, relay, PlayerId.None, 270f, 0) },
                new[] { new MapLane(new[] { relay - turn * Vector3.right * 500f, relay + turn * Vector3.right * 500f }) },
                settings, size, new Random(7), System.Array.Empty<HazardShape>());
            Assert.That(density.IsField(sample), Is.False);
        }

        [Test]
        public void ArchetypeWithRequiredHazards_RejectsMissingPresets()
        {
            MapGenerationSettings settings = Object.Instantiate(
                AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH));
            try
            {
                SerializedObject serialized = new SerializedObject(settings);
                serialized.FindProperty("hazards").arraySize = 0;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                MapLayoutGenerator generator = new MapLayoutGenerator(settings, new MapFeatureRadii(270f, 120f, 540f));
                PlayerSlot[] players = CreateDuel();
                Assert.That(() => generator.Generate(MapSize.Medium, players, new Random(7), 7, PlanetType.Hoth),
                    Throws.InvalidOperationException.With.Message.Contains("no hazard presets"));
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [Test]
        public void TwinPass_MainRelayRouteExitsThroughFlank()
        {
            MapSizeSettings size = GetSize(PlanetType.Mandalore);
            Vector3 center = PlanarGeometry.GetCenter(size.Bounds);
            float half = (size.Bounds.Max.x - size.Bounds.Min.x) * 0.5f;
            Quaternion turn = Quaternion.Euler(0f, size.SkeletonAngle, 0f);
            Vector3 endpoint = center + turn * new Vector3(0f, 0f, half * 0.9f);
            List<MapLane> lanes = new List<MapLane> { new MapLane(new[] { center, endpoint }) };
            ArchetypeLaneRouter.Route(size, lanes);
            Assert.That(lanes[0].Points[0], Is.EqualTo(center));
            Assert.That(lanes[0].Points.Last(), Is.EqualTo(endpoint));
            Assert.That(lanes[0].Points.Count, Is.GreaterThan(2));
            foreach (Vector3 point in lanes[0].Points.Skip(1).Take(lanes[0].Points.Count - 2))
                Assert.That(Mathf.Abs((Quaternion.Inverse(turn) * (point - center)).x),
                    Is.GreaterThan(half * 0.5f + size.MinLaneWidth * 0.5f));
        }

        [TestCase(-1f)]
        [TestCase(1f)]
        public void TwinPass_MainAccessUsesTheNearFlank(float side)
        {
            MapSizeSettings size = GetSize(PlanetType.Mandalore);
            Vector3 center = PlanarGeometry.GetCenter(size.Bounds);
            float half = (size.Bounds.Max.x - size.Bounds.Min.x) * 0.5f;
            Quaternion turn = Quaternion.Euler(0f, size.SkeletonAngle, 0f);
            Vector3 start = center + turn * new Vector3(side * half * 0.75f, 0f, half * 0.6f);
            List<MapLane> lanes = new List<MapLane> { new MapLane(new[] { start, center }) };
            ArchetypeLaneRouter.Route(size, lanes);
            Assert.That(lanes[0].Points.Count, Is.EqualTo(4));
            foreach (Vector3 point in lanes[0].Points.Skip(1).Take(2))
                Assert.That(Mathf.Sign((Quaternion.Inverse(turn) * (point - center)).x), Is.EqualTo(side));
        }

        [Test]
        public void PlanetMacroSettings_AreStableAcrossRosterScaling()
        {
            MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            PlanetMapProfileEntry profile = settings.Profiles.Get(PlanetType.Hoth);
            MapSizeSettings duel = settings.GetSize(MapSize.Large).ForArchetype(profile, 2);
            MapSizeSettings full = settings.GetSize(MapSize.Large).ForArchetype(profile, 8);
            Assert.That(full.PlanetSeed, Is.EqualTo(duel.PlanetSeed));
            Assert.That(full.SkeletonAngle, Is.EqualTo(duel.SkeletonAngle));
            Assert.That(full.Archetype, Is.EqualTo(duel.Archetype));
            Assert.That(full.Bounds.Max.x - full.Bounds.Min.x,
                Is.GreaterThan(duel.Bounds.Max.x - duel.Bounds.Min.x));
            Assert.That(settings.GetSize(MapSize.Large).IsArchetypeLayout, Is.False,
                "Per-match scaling must not mutate the shared settings asset.");
        }

        [Test, Category("MapGeneration"), Explicit]
        public void SamePlanetAndRunSeed_ReproduceGeometry_AnotherRunChangesDetails()
        {
            MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            MapLayoutGenerator generator = new MapLayoutGenerator(settings, new MapFeatureRadii(270f, 120f, 540f));
            PlayerSlot[] players =
            {
                new PlayerSlot(new PlayerId(0), new TeamId(0), FactionType.Republic,
                    PlayerController.Human, EnemyAiDifficulty.Medium, 0),
                new PlayerSlot(new PlayerId(1), new TeamId(1), FactionType.Separatist,
                    PlayerController.Ai, EnemyAiDifficulty.Medium, 1)
            };
            MapLayout first = generator.Generate(MapSize.Medium, players, new Random(7), 7, PlanetType.Hoth);
            MapLayout repeated = generator.Generate(MapSize.Medium, players, new Random(7), 7, PlanetType.Hoth);
            MapLayout varied = generator.Generate(MapSize.Medium, players, new Random(19), 19, PlanetType.Hoth);
            Assert.That(repeated.Zones.Select(zone => zone.Center), Is.EqualTo(first.Zones.Select(zone => zone.Center)));
            Assert.That(repeated.Sites.Select(site => site.Center), Is.EqualTo(first.Sites.Select(site => site.Center)));
            Assert.That(repeated.Lanes.SelectMany(lane => lane.Points), Is.EqualTo(first.Lanes.SelectMany(lane => lane.Points)));
            Assert.That(repeated.Fields.SelectMany(field => field.Volumes).Select(volume => volume.Center),
                Is.EqualTo(first.Fields.SelectMany(field => field.Volumes).Select(volume => volume.Center)));
            Assert.That(varied.Lanes.SelectMany(lane => lane.Points),
                Is.Not.EqualTo(first.Lanes.SelectMany(lane => lane.Points)));
        }

        [Test, Category("MapGeneration"), Explicit]
        public void TwinPass_PreservesPhysicalWallsAfterLaneCarving()
        {
            MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            MapLayoutGenerator generator = new MapLayoutGenerator(settings, new MapFeatureRadii(270f, 120f, 540f));
            PlayerSlot[] players =
            {
                new PlayerSlot(new PlayerId(0), new TeamId(0), FactionType.Republic,
                    PlayerController.Human, EnemyAiDifficulty.Medium, 0),
                new PlayerSlot(new PlayerId(1), new TeamId(1), FactionType.Separatist,
                    PlayerController.Ai, EnemyAiDifficulty.Medium, 1)
            };
            MapLayout layout = generator.Generate(MapSize.Medium, players, new Random(137), 137, PlanetType.AbregadoRae);
            MapSizeSettings size = settings.GetSize(MapSize.Medium).ForArchetype(
                settings.Profiles.Get(PlanetType.AbregadoRae), players.Length);
            size.ConfigureStructuralFields(540f, settings.ZoneClearance);
            Assert.That(size.StructuralFields.Count, Is.EqualTo(2));
            foreach (FieldVolume wall in size.StructuralFields)
                Assert.That(layout.Fields.SelectMany(field => field.Volumes).Any(volume =>
                    PlanarGeometry.Distance(volume.Center, wall.Center) < volume.Radius), Is.True,
                    "The central wall must survive field rasterization and lane carving.");
        }

        [Test, Category("MapGeneration"), Explicit]
        public void BeltPasses_RemainOpenInFinalFieldVolumes()
        {
            MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            MapSizeSettings size = GetSize(PlanetType.Hoth);
            MapLayoutGenerator generator = new MapLayoutGenerator(settings, new MapFeatureRadii(270f, 120f, 540f));
            MapLayout layout = generator.Generate(MapSize.Medium, CreateDuel(), new Random(7), 7, PlanetType.Hoth);
            Vector3 center = PlanarGeometry.GetCenter(size.Bounds);
            float half = (size.Bounds.Max.x - size.Bounds.Min.x) * 0.5f;
            Quaternion turn = Quaternion.Euler(0f, size.SkeletonAngle, 0f);
            foreach (float pass in new[] { -1f, 0f, 1f })
            for (int across = -2; across <= 2; across++)
            for (int along = -4; along <= 4; along++)
            {
                Vector3 point = center + turn * new Vector3(pass * half * 0.62f + across * size.MinLaneWidth * 0.25f,
                    0f, along * half * 0.05f);
                Assert.That(layout.Fields.SelectMany(field => field.Volumes).Any(volume =>
                    PlanarGeometry.Distance(point, volume.Center) < volume.Radius), Is.False,
                    $"Pass {pass}, across {across}, along {along} was filled after rasterization or sealed-space filling.");
            }
        }

        private static PlayerSlot[] CreateDuel() => new[]
        {
            new PlayerSlot(new PlayerId(0), new TeamId(0), FactionType.Republic,
                PlayerController.Human, EnemyAiDifficulty.Medium, 0),
            new PlayerSlot(new PlayerId(1), new TeamId(1), FactionType.Separatist,
                PlayerController.Ai, EnemyAiDifficulty.Medium, 1)
        };

        private static MapSizeSettings GetSize(PlanetType planet)
        {
            MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            return settings.GetSize(MapSize.Medium).ForArchetype(settings.Profiles.Get(planet), 2);
        }
    }
}
