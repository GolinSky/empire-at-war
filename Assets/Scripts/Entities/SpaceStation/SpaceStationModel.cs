using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Movement;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Ship;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Entities.SpaceStation
{
    public interface ISpaceStationModelObserver:IModelObserver, IUnitModelObserver
    {
    }
    [CreateAssetMenu(fileName = "SpaceStationModel", menuName = "Model/SpaceStationModel")]
    public class SpaceStationModel:Model, ISpaceStationModelObserver
    {
        [field:SerializeField] public HealthModel HealthModel { get; private set; }
        [field:SerializeField] public RadarModel RadarModel { get; private set; }
        [field:SerializeField] public DefaultMoveModel DefaultMoveModel { get; private set; }
        [field:SerializeField] public AttackModel AttackModel { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            AddInnerModels(HealthModel, RadarModel, DefaultMoveModel, AttackModel);
        }
    }
}