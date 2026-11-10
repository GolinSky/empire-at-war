using System;
using EmpireAtWar.Components.Ship.Health;

namespace EmpireAtWar.Models.Health
{
    /// <summary>Read-only view of a <see cref="HardPointModel"/>; only the owning health model changes hardpoint health.</summary>
    public interface IReadOnlyHardPointModel
    {
        event Action OnHardPointHealthChanged;
        event Action OnDestroyedByDamage;

        int Id { get; }
        HardPointType HardPointType { get; }
        bool IsInstalled { get; }
        bool IsDestroyed { get; }
        float HealthPercentage { get; }
        float HullDamageMultiplier { get; }
    }
}
