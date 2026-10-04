using System.Collections.Generic;
using System.Threading;
using EmpireAtWar.Components.Obstacles;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Map.Generation;
using EmpireAtWar.Views.ReinforcementZones;
using UnityEngine;
using ViewComponents;

namespace EmpireAtWar.Entities.Map
{
    /// <summary>Spawns a generated <see cref="MapLayout"/>: zones, capture sites, asteroid fields, border and fog area.</summary>
    public sealed class MapLayoutView : MonoBehaviour
    {
        private const float FRAME_SLICE_SECONDS = 0.008f;
        private const float FIELD_VOLUME_MESH_HEIGHT = 2f;

        [SerializeField] private ReinforcementZoneView zonePrefab;
        [SerializeField] private CaptureSiteView miningSitePrefab;
        [SerializeField] private CaptureSiteView battleSitePrefab;
        [SerializeField, Tooltip("Invisible impassable cylinder: unit-diameter, two-unit-tall convex collider.")]
        private MapObstacle fieldVolumePrefab;
        [SerializeField, Min(1f), Tooltip("Height of a field volume. Ships whose hull lies wholly below or above it pass the field.")]
        private float fieldVolumeHeight = 80f;
        [SerializeField] private GameObject largeRockPrefab;
        [SerializeField] private GameObject mediumRockPrefab;
        [SerializeField] private GameObject[] debrisRockPrefabs;
        [SerializeField] private Transform zoneRoot;
        [SerializeField] private Transform siteRoot;
        [SerializeField] private Transform fieldRoot;
        [SerializeField] private LineRenderer borderLine;
        [SerializeField] private FogOfWarSystem fogOfWarSystem;
        private readonly List<MapObstacle> _obstacles = new List<MapObstacle>();

        [SerializeField] private float borderHeight;
        [SerializeField, Min(1f), Tooltip("Map side the fog plane was authored for.")]
        private float fogReferenceSide = 600f;

        public MapFeatureRadii FeatureRadii =>
            new MapFeatureRadii(zonePrefab.Radius, miningSitePrefab.Radius, battleSitePrefab.Radius);

        public ReinforcementZoneView[] ZoneViews { get; private set; }
        public CaptureSiteView[] SiteViews { get; private set; }
        public IReadOnlyList<MapObstacle> Obstacles => _obstacles;

        /// <summary>Spawns the layout over several frames, at most <see cref="FRAME_SLICE_SECONDS"/> per frame.</summary>
        public async Awaitable BuildAsync(MapLayout layout, CancellationToken cancellationToken)
        {
            // Fog first: spawned colliders are hover-tested against the fog from their first frame.
            fogOfWarSystem.InitializeArea((layout.SizeRange.Max.x - layout.SizeRange.Min.x) / fogReferenceSide);
            DrawBorder(layout);
            float sliceStart = Time.realtimeSinceStartup;

            ZoneViews = new ReinforcementZoneView[layout.Zones.Count];
            for (int i = 0; i < ZoneViews.Length; i++)
            {
                ZoneSpot zone = layout.Zones[i];
                ZoneViews[i] = Instantiate(zonePrefab, zone.Center, Quaternion.identity, zoneRoot);
                ZoneViews[i].Configure(zone.Owner, zone.IsCapturable);
                sliceStart = await YieldWhenSliceSpent(sliceStart, cancellationToken);
            }

            SiteViews = new CaptureSiteView[layout.Sites.Count];
            for (int i = 0; i < SiteViews.Length; i++)
            {
                SiteSpot site = layout.Sites[i];
                CaptureSiteView prefab = site.FacilityType == SiteFacilityType.Mining
                    ? miningSitePrefab
                    : battleSitePrefab;
                SiteViews[i] = Instantiate(prefab, site.Center, Quaternion.identity, siteRoot);
                _obstacles.AddRange(SiteViews[i].RockObstacles);
                sliceStart = await YieldWhenSliceSpent(sliceStart, cancellationToken);
            }

            for (int i = 0; i < layout.Fields.Count; i++)
            {
                AsteroidField field = layout.Fields[i];
                Transform root = new GameObject($"AsteroidField_{i}").transform;
                root.SetParent(fieldRoot, false);
                foreach (FieldVolume volume in field.Volumes)
                {
                    MapObstacle obstacle = Instantiate(fieldVolumePrefab, volume.Center, Quaternion.identity, root);
                    obstacle.transform.localScale = new Vector3(volume.Radius * 2f,
                        fieldVolumeHeight / FIELD_VOLUME_MESH_HEIGHT, volume.Radius * 2f);
                    _obstacles.Add(obstacle);
                }

                for (int j = 0; j < field.Rocks.Count; j++)
                {
                    SpawnRock(field.Rocks[j], j, root);
                    sliceStart = await YieldWhenSliceSpent(sliceStart, cancellationToken);
                }
            }
        }

        private static async Awaitable<float> YieldWhenSliceSpent(float sliceStart, CancellationToken cancellationToken)
        {
            if (Time.realtimeSinceStartup - sliceStart < FRAME_SLICE_SECONDS)
            {
                return sliceStart;
            }

            await Awaitable.NextFrameAsync(cancellationToken);
            return Time.realtimeSinceStartup;
        }

        private void SpawnRock(AsteroidSpot rock, int index, Transform root)
        {
            GameObject prefab = rock.Size switch
            {
                AsteroidSize.Large => largeRockPrefab,
                AsteroidSize.Medium => mediumRockPrefab,
                _ => debrisRockPrefabs[index % debrisRockPrefabs.Length]
            };
            GameObject instance = Instantiate(prefab, rock.Position, Quaternion.Euler(rock.Rotation), root);
            instance.transform.localScale = prefab.transform.localScale * rock.Scale;
        }

        private void DrawBorder(MapLayout layout)
        {
            Vector2 min = layout.SizeRange.Min;
            Vector2 max = layout.SizeRange.Max;
            borderLine.loop = true;
            borderLine.positionCount = 4;
            borderLine.SetPosition(0, new Vector3(min.x, borderHeight, min.y));
            borderLine.SetPosition(1, new Vector3(min.x, borderHeight, max.y));
            borderLine.SetPosition(2, new Vector3(max.x, borderHeight, max.y));
            borderLine.SetPosition(3, new Vector3(max.x, borderHeight, min.y));
        }
    }
}
