using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using UnityEngine;

namespace EmpireAtWar.Services.Repair
{
    /// <summary>A unit that repairs allies; <see cref="RepairService"/> picks its targets and applies the repair.</summary>
    public interface IRepairSourceFacade : IEntityFacade
    {
        RepairProfile Profile { get; }
        Vector3 Position { get; }

        /// <summary>Alive, fully built and not ion-disabled.</summary>
        bool CanRepair { get; }

        IReadOnlyList<IEntity> Targets { get; }
        event Action TargetsChanged;
        void SetTargets(IReadOnlyList<IEntity> targets);
    }
}
