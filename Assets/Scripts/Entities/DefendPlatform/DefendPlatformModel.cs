using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Movement;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Ship;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Entities.DefendPlatform
{
    public interface IDefendPlatformModelObserver : IUnitModelObserver
    {

    }

    [CreateAssetMenu(fileName = "DefendPlatformModel", menuName = "Model/DefendPlatformModel")]
    public class DefendPlatformModel : Model, IDefendPlatformModelObserver
    {
        [field: SerializeField] public EntityComponentData ComponentData { get; private set; }
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
