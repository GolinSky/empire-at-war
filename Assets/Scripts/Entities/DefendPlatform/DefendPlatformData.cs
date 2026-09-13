using EmpireAtWar.Components.Radar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Ship;
using EmpireAtWar.Mvc;
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
        [field:SerializeField] public RadarModel RadarModel { get; private set; }
    }
}
