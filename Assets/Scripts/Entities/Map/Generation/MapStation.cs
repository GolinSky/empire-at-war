using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Entities.Map.Generation
{
    public readonly struct MapStation
    {
        public MapStation(FactionType faction, PlayerId owner, Vector3 position, float radius)
        {
            Faction = faction;
            Owner = owner;
            Position = position;
            Radius = radius;
        }

        public FactionType Faction { get; }
        public PlayerId Owner { get; }
        public Vector3 Position { get; }
        public float Radius { get; }
    }
}
