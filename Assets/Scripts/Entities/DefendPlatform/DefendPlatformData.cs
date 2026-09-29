using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Ship;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.UnitWreck;
using UnityEngine;

namespace EmpireAtWar.Entities.DefendPlatform
{
    public interface IDefendPlatformModelObserver : IUnitModelObserver
    {

    }

    [CreateAssetMenu(fileName = nameof(DefendPlatformData), menuName = "Data/DefendPlatformData")]
    public class DefendPlatformData : Data, IModel, IDefendPlatformModelObserver
    {
        [field: SerializeField] public EntityComponentData ComponentData { get; private set; }
        [Tooltip("Optional. Empty = the platform is only removed; set = a breaking wreck replaces it.")]
        [field: SerializeField] public UnitWreckData Wreck { get; private set; }
    }
}
