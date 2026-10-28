using System.Collections.Generic;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.SkirmishCamera;
using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    /// <summary>The generated battlefield of one skirmish; immutable once built.</summary>
    public sealed class MapLayout : IMapModelObserver
    {
        private readonly IReadOnlyDictionary<PlayerId, Vector3> _stationPositions;

        public Vector2Range SizeRange { get; }
        /// <summary>Ground-plane point the planet sits under; each planet keeps its authored depth.</summary>
        public Vector3 PlanetPosition { get; }
        public IReadOnlyList<ZoneSpot> Zones { get; }
        public IReadOnlyList<SiteSpot> Sites { get; }
        public IReadOnlyList<MapLane> Lanes { get; }
        public IReadOnlyList<AsteroidField> Fields { get; }
        public IReadOnlyList<HazardShape> Hazards { get; }
        public IReadOnlyList<AsteroidSpot> DecorRocks { get; }
        public float EdgeMargin { get; }

        public MapLayout(
            IReadOnlyDictionary<PlayerId, Vector3> stationPositions,
            IReadOnlyList<ZoneSpot> zones,
            IReadOnlyList<SiteSpot> sites,
            IReadOnlyList<MapLane> lanes,
            IReadOnlyList<AsteroidField> fields,
            Vector2Range sizeRange,
            Vector3 planetPosition)
            : this(stationPositions, zones, sites, lanes, fields, sizeRange, planetPosition,
                System.Array.Empty<HazardShape>())
        {
        }

        public MapLayout(
            IReadOnlyDictionary<PlayerId, Vector3> stationPositions,
            IReadOnlyList<ZoneSpot> zones,
            IReadOnlyList<SiteSpot> sites,
            IReadOnlyList<MapLane> lanes,
            IReadOnlyList<AsteroidField> fields,
            Vector2Range sizeRange,
            Vector3 planetPosition,
            IReadOnlyList<HazardShape> hazards,
            IReadOnlyList<AsteroidSpot> decorRocks = null,
            float edgeMargin = 0f)
        {
            SizeRange = sizeRange;
            _stationPositions = stationPositions;
            PlanetPosition = planetPosition;
            Zones = zones;
            Sites = sites;
            Lanes = lanes;
            Fields = fields;
            Hazards = hazards;
            DecorRocks = decorRocks ?? System.Array.Empty<AsteroidSpot>();
            EdgeMargin = edgeMargin;
        }

        public Vector3 GetStationPosition(PlayerId owner)
        {
            return _stationPositions[owner];
        }
    }
}
