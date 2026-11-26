using System.Numerics;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Players;

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
            Vector3 position,
            ShipClass shipClass,
            PlayerId owner,
            float secondsSinceDamaged,
            long id)
        {
            Id = id;
            Position = position;
            ShipClass = shipClass;
            Owner = owner;
            SecondsSinceDamaged = secondsSinceDamaged;
        }
    }
}
