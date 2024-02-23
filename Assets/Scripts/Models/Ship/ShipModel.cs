using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Radar;
using EmpireAtWar.Models.Weapon;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Models.Ship
{
    public interface IShipModelObserver : IModelObserver
    {
        ParticleSystem DeathExplosionVfx { get; }
        ShipType ShipType { get; }

        ShipInfoUi FillWithData(ShipInfoUi shipInfoUi);

    }

    [CreateAssetMenu(fileName = "ShipModel", menuName = "Model/ShipModel")]
    public class ShipModel : Model, IShipModelObserver
    {
        [Header("Move Model")]
        [SerializeField] private MoveModel moveModel;
        
        [Header("Health Model")]
        [SerializeField] private HealthModel healthModel;

        [Header("Weapon Model")] 
        [SerializeField] private WeaponModel weaponModel;
        
        [Header("Radar Model")] 
        [SerializeField] private RadarModel radarModel;

        [field:SerializeField] public ParticleSystem DeathExplosionVfx { get; private set; }
        
        [field:SerializeField] public float MinMoveCoefficient { get; private set; }

        
        [Inject]
        public ShipType ShipType { get; }

        public MoveModel MoveModel => moveModel;

        public HealthModel HealthModel => healthModel;

        public WeaponModel WeaponModel => weaponModel;

        public RadarModel RadarModel => radarModel;


        protected override void Awake()
        {
            base.Awake();
            AddInnerModels(moveModel, healthModel, weaponModel, radarModel);
        }

        public ShipInfoUi FillWithData(ShipInfoUi shipInfoUi)
        {
            shipInfoUi.Init(healthModel.ShipUnitModels);
            return shipInfoUi;
        }
    }
}