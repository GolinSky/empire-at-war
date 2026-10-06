using UnityEngine;

namespace EmpireAtWar.Entities.Ship.Abilities
{
    public sealed class CompositeBeamFacade : ICompositeBeamFacade
    {
        public Transform Muzzle { get; }

        public CompositeBeamFacade(EmpireAtWar.Ship.Ship ship)
        {
            Muzzle = ship.CompositeBeamMuzzle;
        }
    }
}
