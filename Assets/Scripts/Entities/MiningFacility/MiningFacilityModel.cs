using EmpireAtWar.Components.Radar;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Ship;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Entities.MiningFacility
{
    public interface IMiningFacilityModelObserver : IUnitModelObserver
    {

    }

    [CreateAssetMenu(fileName = "MiningFacilityModel", menuName = "Model/MiningFacilityModel")]
    public class MiningFacilityModel : Model, IMiningFacilityModelObserver
    {
        [field:SerializeField] public HealthModel HealthModel { get; private set; }
        [field:SerializeField] public RadarModel RadarModel { get; private set; }
        [field:SerializeField] public DefaultMoveModel DefaultMoveModel { get; private set; }

        [field:SerializeField] public float Income { get; private set; }
        
        protected override void Awake()
        {
            base.Awake();
            AddInnerModels(HealthModel, RadarModel, DefaultMoveModel);
        }
    }
}