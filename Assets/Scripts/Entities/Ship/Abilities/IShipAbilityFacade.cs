using System.Collections.Generic;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Health;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.Abilities
{
    public interface IShipAbilityFacade : IEntityFacade
    {
        IReadOnlyList<ShipAbilitySlot> Slots { get; }
        CombatModifiers Modifiers { get; }
        Vector3 WorldPosition { get; }
        IEntity Entity { get; }
        IHealthModelObserver Health { get; }
        float RadarRange { get; }
    }
}
