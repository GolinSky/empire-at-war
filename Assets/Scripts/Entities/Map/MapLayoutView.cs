using System.Collections.Generic;
using EmpireAtWar.Components.Obstacles;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Map.Generation;
using EmpireAtWar.Views.ReinforcementZones;
using UnityEngine;
using ViewComponents;

namespace EmpireAtWar.Entities.Map
{
    /// <summary>Spawns a generated <see cref="MapLayout"/>: zones, capture sites, asteroid walls, border and fog area.</summary>
    public sealed class MapLayoutView : MonoBehaviour
    {
        [SerializeField] private ReinforcementZoneView zonePrefab;
        [SerializeField] private CaptureSiteView miningSitePrefab;
        [SerializeField] private CaptureSiteView battleSitePrefab;
        [SerializeField] private MapObstacle obstaclePrefab;
        [SerializeField] private Transform zoneRoot;
        [SerializeField] private Transform siteRoot;
        [SerializeField] private Transform obstacleRoot;
        [SerializeField] private LineRenderer borderLine;
        [SerializeField] private float borderHeight;
        [SerializeField] private FogOfWarSystem fogOfWarSystem;
        [SerializeField, Min(1f), Tooltip("Map side the fog plane was authored for.")]
        private float fogReferenceSide = 600f;

        public MapFeatureRadii FeatureRadii =>
            new MapFeatureRadii(zonePrefab.Radius, miningSitePrefab.Radius, battleSitePrefab.Radius);

        public ReinforcementZoneView[] ZoneViews { get; private set; }
        public CaptureSiteView[] SiteViews { get; private set; }
        public IReadOnlyList<MapObstacle> Obstacles => _obstacles;

        private readonly List<MapObstacle> _obstacles = new List<MapObstacle>();

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

            foreach (ObstacleSpot spot in layout.Obstacles)
            {
                MapObstacle obstacle = Instantiate(
                    obstaclePrefab, spot.Position, Quaternion.Euler(0f, spot.Yaw, 0f), obstacleRoot);
                obstacle.transform.localScale = obstaclePrefab.transform.localScale * spot.Scale;
                _obstacles.Add(obstacle);
            }

            DrawBorder(layout);
            fogOfWarSystem.ScaleArea((layout.SizeRange.Max.x - layout.SizeRange.Min.x) / fogReferenceSide);
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
