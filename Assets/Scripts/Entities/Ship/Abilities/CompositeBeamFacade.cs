using UnityEngine;
using ShipEntity = EmpireAtWar.Ship.Ship;

namespace EmpireAtWar.Entities.Ship.Abilities
{
    public sealed class CompositeBeamFacade : ICompositeBeamFacade
    {
        public Transform Muzzle { get; }

        public CompositeBeamFacade(ShipEntity ship)
        {
            Muzzle = ship.CompositeBeamMuzzle;
        }
    }
}
