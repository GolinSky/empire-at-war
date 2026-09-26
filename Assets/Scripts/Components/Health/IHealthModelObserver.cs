using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Models.Health
{
    public interface IHealthModelObserver : IModelObserver
    {
        event Action OnDestroy;
        event Action OnValueChanged;

        ShipClass ShipClass { get; }
        HardPointModel[] HardPointModels { get; }
        float Hull { get; }
        float HullPercentage { get; }
        float Shields { get; }
        float ShieldPercentage { get; }
        bool IsDestroyed { get; }
        bool IsLostShieldGenerator { get; }
        bool HasUnits { get; }
        bool HasLiveHardPoints { get; }
        bool HasShields { get; }
        IHardPointModel[] GetShipUnits(HardPointType hardPointType);
        PlayerType PlayerType { get; }
    }
}
