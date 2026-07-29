using EmpireAtWar.Components.Radar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Ship;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Entities.MiningFacility
{
    public interface IMiningFacilityModelObserver : IUnitModelObserver
    {

    }

    [CreateAssetMenu(fileName = "MiningFacilityModel", menuName = "Model/MiningFacilityModel")]
    public class MiningFacilityModel : Model, IMiningFacilityModelObserver
    {
        [field: SerializeField] public EntityComponentData ComponentData { get; private set; }
        [field:SerializeField] public HealthModel HealthModel { get; private set; }
        [field:SerializeField] public RadarModel RadarModel { get; private set; }

        [field:SerializeField] public float Income { get; private set; }
        IHealthModelObserver IUnitModelObserver.HealthModel => HealthModel;
    }
}
