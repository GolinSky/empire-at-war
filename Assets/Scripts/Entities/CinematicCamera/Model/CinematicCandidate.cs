using System.Numerics;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Entities.CinematicCamera.Model
{
    public readonly struct CinematicCandidate
    {
        public long Id { get; }
        public Vector3 Position { get; }
        public ShipClass ShipClass { get; }
        public PlayerType PlayerType { get; }
        public float SecondsSinceDamaged { get; }

        public CinematicCandidate(
            long id,
            Vector3 position,
            ShipClass shipClass,
            PlayerType playerType,
            float secondsSinceDamaged)
        {
            Id = id;
            Position = position;
            ShipClass = shipClass;
            PlayerType = playerType;
            SecondsSinceDamaged = secondsSinceDamaged;
        }
    }
}
