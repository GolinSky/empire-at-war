using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Entities.Map.Generation
{
    public readonly struct MapStation
    {
        public MapStation(FactionType faction, PlayerType owner, Vector3 position, float radius)
        {
            Faction = faction;
            Owner = owner;
            Position = position;
            Radius = radius;
        }

        public FactionType Faction { get; }
        public PlayerType Owner { get; }
        public Vector3 Position { get; }
        public float Radius { get; }
    }
}
