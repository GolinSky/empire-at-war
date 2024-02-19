using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Radar;
using EmpireAtWar.Models.Weapon;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Ship
{
    public interface IShipModelObserver : IModelObserver
    {
        Sprite ShipIcon { get; }
        ShipInfoUi ShipInfoUi { get; }
        ParticleSystem DeathExplosionVfx { get; }
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
        [field: SerializeField] public Sprite ShipIcon { get; private set; }

        [SerializeField] private ShipInfoUi shipUiPrefab;
        private ShipInfoUi shipInfoUi;

        [field:SerializeField] public ParticleSystem DeathExplosionVfx { get; private set; }

        public ShipInfoUi ShipInfoUi
        {
            get
            {
                if (shipInfoUi == null)
                {
                    shipInfoUi = Instantiate(shipUiPrefab);
                    shipInfoUi.Init(healthModel.ShipUnitModels);
                }
                return shipInfoUi;
            }
        }

        
        protected override void Awake()
        {
            base.Awake();
            AddInnerModels(moveModel, healthModel, weaponModel, radarModel);
        }

    }
}