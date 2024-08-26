using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Radar;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.Ship;
using UnityEngine;
using LightWeightFramework.Model;

namespace EmpireAtWar.Models.DefendPlatform
{
    public interface IDefendPlatformModelObserver : IUnitModelObserver
    {

    }

    [CreateAssetMenu(fileName = "DefendPlatformModel", menuName = "Model/DefendPlatformModel")]
    public class DefendPlatformModel : Model, IDefendPlatformModelObserver
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