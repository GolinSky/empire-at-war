using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Models.Health
{
    public interface IHealthModelObserver : IModelObserver
    {
        event Action OnDestroy;
        event Action OnValueChanged;

        HardPointModel[] HardPointModels { get; }
        float Armor { get; }
        float ArmorPercentage { get; }
        float Shields { get; }
        float ShieldPercentage { get; }
        bool IsDestroyed { get; }
        bool IsLostShieldGenerator { get; }
        bool HasUnits { get; }
        bool HasShields { get; }
        IHardPointModel[] GetShipUnits(HardPointType hardPointType);
        PlayerType PlayerType { get; }
        Transform Transform { get; }
    }
}
