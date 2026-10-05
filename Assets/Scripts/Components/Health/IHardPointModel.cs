using System;
using EmpireAtWar.Components.Ship.Health;
using UnityEngine;

namespace EmpireAtWar.Models.Health
{
    public interface IHardPointModel
    {
        event Action OnHardPointHealthChanged;

        event Action OnDestroyed;

        HardPointType HardPointType { get; }
        float HealthPercentage { get; }
        int Id { get; }
        int Generation { get; }
        bool IsDestroyed { get; }
        Vector3 Position { get; }
        Transform Transform { get; }
        /// <summary>The transform that moves the whole unit; unlike <see cref="Transform"/> it does not swing when the unit turns.</summary>
        Transform Pivot { get; }
    }
}
