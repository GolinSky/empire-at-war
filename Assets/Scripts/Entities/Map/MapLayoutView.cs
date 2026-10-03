using System.Collections.Generic;
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
        [SerializeField] private ReinforcementZoneView zonePrefab;
        [SerializeField] private CaptureSiteView miningSitePrefab;
        [SerializeField] private CaptureSiteView battleSitePrefab;
        [SerializeField, Tooltip("Invisible impassable circle with a unit-diameter collider.")]
        private MapObstacle fieldVolumePrefab;
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

        public void Build(MapLayout layout)
        {
            ZoneViews = new ReinforcementZoneView[layout.Zones.Count];
            for (int i = 0; i < ZoneViews.Length; i++)
            {
                ZoneSpot zone = layout.Zones[i];
                ZoneViews[i] = Instantiate(zonePrefab, zone.Center, Quaternion.identity, zoneRoot);
                ZoneViews[i].Configure(zone.Owner, zone.IsCapturable);
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
            }

            for (int i = 0; i < layout.Fields.Count; i++)
            {
                BuildField(layout.Fields[i], i);
            }

            DrawBorder(layout);
            fogOfWarSystem.ScaleArea((layout.SizeRange.Max.x - layout.SizeRange.Min.x) / fogReferenceSide);
        }

        private void BuildField(AsteroidField field, int index)
        {
            Transform root = new GameObject($"AsteroidField_{index}").transform;
            root.SetParent(fieldRoot, false);
            foreach (FieldVolume volume in field.Volumes)
            {
                MapObstacle obstacle = Instantiate(fieldVolumePrefab, volume.Center, Quaternion.identity, root);
                obstacle.transform.localScale = Vector3.one * volume.Radius * 2f;
                _obstacles.Add(obstacle);
            }

            for (int i = 0; i < field.Rocks.Count; i++)
            {
                AsteroidSpot rock = field.Rocks[i];
                GameObject prefab = rock.Size switch
                {
                    AsteroidSize.Large => largeRockPrefab,
                    AsteroidSize.Medium => mediumRockPrefab,
                    _ => debrisRockPrefabs[i % debrisRockPrefabs.Length]
                };
                GameObject instance = Instantiate(prefab, rock.Position, Quaternion.Euler(rock.Rotation), root);
                instance.transform.localScale = prefab.transform.localScale * rock.Scale;
            }
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
