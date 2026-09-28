using System.Numerics;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Ship.Health;

namespace EmpireAtWar.Entities.CinematicCamera.Model
{
    public readonly struct CinematicCandidate
    {
        public long Id { get; }
        public Vector3 Position { get; }
        public ShipClass ShipClass { get; }
        public PlayerId Owner { get; }
        public float SecondsSinceDamaged { get; }

        public CinematicCandidate(
            long id,
            Vector3 position,
            ShipClass shipClass,
            PlayerId owner,
            float secondsSinceDamaged)
        {
            Id = id;
            Position = position;
            ShipClass = shipClass;
            Owner = owner;
            SecondsSinceDamaged = secondsSinceDamaged;
        }
    }
}
