using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Radar;
using EmpireAtWar.Models.Ship;
using UnityEngine;
using LightWeightFramework.Model;

namespace EmpireAtWar.Models.MiningFacility
{
    public interface IMiningFacilityModelObserver : IModelObserver, IUnitModelObserver
    {

    }

    [CreateAssetMenu(fileName = "MiningFacilityModel", menuName = "Model/MiningFacilityModel")]
    public class MiningFacilityModel : Model, IMiningFacilityModelObserver
    {
        [field:SerializeField] public HealthModel HealthModel { get; private set; }
        [field:SerializeField] public RadarModel RadarModel { get; private set; }
        [field:SerializeField] public SimpleMoveModel SimpleMoveModel { get; private set; }

        [field:SerializeField] public float Income { get; private set; }
        
        protected override void Awake()
        {
            base.Awake();
            AddInnerModels(HealthModel, RadarModel, SimpleMoveModel);
        }
    }
}