using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.SkirmishCamera;
using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    /// <summary>The generated battlefield of one skirmish; immutable once built.</summary>
    public sealed class MapLayout : IMapModelObserver
    {
        private readonly IReadOnlyDictionary<FactionType, Vector3> _stationPositions;

        public MapLayout(
            Vector2Range sizeRange,
            IReadOnlyDictionary<FactionType, Vector3> stationPositions,
            Vector3 planetPosition,
            IReadOnlyList<ZoneSpot> zones,
            IReadOnlyList<SiteSpot> sites,
            IReadOnlyList<MapLane> lanes,
            IReadOnlyList<AsteroidField> fields)
        {
            SizeRange = sizeRange;
            _stationPositions = stationPositions;
            PlanetPosition = planetPosition;
            Zones = zones;
            Sites = sites;
            Lanes = lanes;
            Fields = fields;
        }

        public Vector2Range SizeRange { get; }
        /// <summary>Ground-plane point the planet sits under; each planet keeps its authored depth.</summary>
        public Vector3 PlanetPosition { get; }
        public IReadOnlyList<ZoneSpot> Zones { get; }
        public IReadOnlyList<SiteSpot> Sites { get; }
        public IReadOnlyList<MapLane> Lanes { get; }
        public IReadOnlyList<AsteroidField> Fields { get; }

        public Vector3 GetStationPosition(FactionType factionType)
        {
            return _stationPositions[factionType];
        }
    }
}
