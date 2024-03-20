using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Radar;
using EmpireAtWar.Models.Weapon;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.SpaceStation
{
    public interface ISpaceStationModelObserver:IModelObserver
    {
    }
    [CreateAssetMenu(fileName = "SpaceStationModel", menuName = "Model/SpaceStationModel")]
    public class SpaceStationModel:Model, ISpaceStationModelObserver
    {
        [field:SerializeField] public HealthModel HealthModel { get; private set; }
        [field:SerializeField] public RadarModel RadarModel { get; private set; }
        [field:SerializeField] public SimpleMoveModel SimpleMoveModel { get; private set; }
        [field:SerializeField] public WeaponModel WeaponModel { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            AddInnerModels(HealthModel, RadarModel, SimpleMoveModel, WeaponModel);
        }
    }
}