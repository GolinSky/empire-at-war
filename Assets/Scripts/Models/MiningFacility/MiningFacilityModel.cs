using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Radar;
using UnityEngine;
using LightWeightFramework.Model;

namespace EmpireAtWar.Models.MiningFacility
{
    public interface IMiningFacilityModelObserver : IModelObserver
    {

    }

    [CreateAssetMenu(fileName = "MiningFacilityModel", menuName = "Model/MiningFacilityModel")]
    public class MiningFacilityModel : Model, IMiningFacilityModelObserver
    {
        [field:SerializeField] public HealthModel HealthModel { get; private set; }
        [field:SerializeField] public RadarModel RadarModel { get; private set; }
        [field:SerializeField] public MoveModel MoveModel { get; private set; }


        protected override void Awake()
        {
            base.Awake();
            AddInnerModels(HealthModel, RadarModel, MoveModel);
        }
    }
}