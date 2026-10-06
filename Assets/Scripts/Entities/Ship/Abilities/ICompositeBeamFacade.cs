using EmpireAtWar.Entities.BaseEntity;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.Abilities
{
    public interface ICompositeBeamFacade : IEntityFacade
    {
        Transform Muzzle { get; }
    }
}
