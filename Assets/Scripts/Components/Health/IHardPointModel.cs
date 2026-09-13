using System;
using EmpireAtWar.Components.Ship.Health;
using UnityEngine;

namespace EmpireAtWar.Models.Health
{
    public interface IHardPointModel
    {
        event Action OnHardPointHealthChanged;
        HardPointType HardPointType { get; }
        float HealthPercentage { get; }
        int Id { get; }
        bool IsDestroyed { get; }
        Vector3 Position { get; }
        Transform Transform { get; }
    }
}
