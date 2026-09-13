using EmpireAtWar.Components.Radar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Ship;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Entities.SpaceStation
{
    public interface ISpaceStationModelObserver:IModelObserver, IUnitModelObserver
    {
    }
    [CreateAssetMenu(fileName = nameof(SpaceStationData), menuName = "Data/SpaceStationData")]
    public class SpaceStationData:Data, IModel, ISpaceStationModelObserver
    {
        [field: SerializeField] public EntityComponentData ComponentData { get; private set; }
        [field:SerializeField] public RadarModel RadarModel { get; private set; }
    }
}
