using System.Collections.Generic;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.Health;
using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IShipAbilityFacade : IEntityFacade
    {
        IReadOnlyList<ShipAbilitySlot> Slots { get; }
        CombatModifiers Modifiers { get; }
        Vector3 WorldPosition { get; }
        IEntity Entity { get; }
        /// <summary>Stable id that stays readable after the entity has left the locator (despawn, teardown).</summary>
        long EntityId => Entity.Id;
        IHealthModelObserver Health { get; }
        float RadarRange { get; }
    }
}
