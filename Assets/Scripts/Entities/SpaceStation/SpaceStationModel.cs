using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Ship;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Entities.SpaceStation
{
    public interface ISpaceStationModelObserver:IModelObserver, IUnitModelObserver
    {
    }
    [CreateAssetMenu(fileName = "SpaceStationModel", menuName = "Model/SpaceStationModel")]
    public class SpaceStationModel:Model, ISpaceStationModelObserver
    {
        [field: SerializeField] public EntityComponentData ComponentData { get; private set; }
        [field:SerializeField] public HealthModel HealthModel { get; private set; }
        [field:SerializeField] public RadarModel RadarModel { get; private set; }
        [field:SerializeField] public AttackModel AttackModel { get; private set; }
        IHealthModelObserver IUnitModelObserver.HealthModel => HealthModel;
    }
}
