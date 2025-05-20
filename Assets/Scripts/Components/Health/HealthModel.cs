using System;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.ViewComponents;
using EmpireAtWar.ViewComponents.Health;
using LightWeightFramework.Model;
using UnityEngine;
using Utilities.ScriptUtils.Math;
using Zenject;

namespace EmpireAtWar.Models.Health
{
    public interface IHealthModelObserver:IModelObserver
    {
        event Action OnDestroy;
        event Action OnValueChanged;

        HardPointModel[] HardPointModels { get; }
        float Armor { get; }
        float Shields { get; }
        float ShieldPercentage { get; }
        bool IsDestroyed { get; }
        bool IsLostShieldGenerator { get; }
        FloatRange ShieldDangerStateRange { get; }

        void InjectDependency(HealthModelDependency healthModelDependency);
        
        
        bool HasUnits { get; }
        IHardPointModel[] GetShipUnits(HardPointType hardPointType);
        PlayerType PlayerType { get; }
        Transform Transform { get; }
    }

    [Serializable]
    public class HealthModel:InnerModel, IHealthModelObserver, IHealthData
    {
        private const float WEAPON_SYSTEM_COEFFICIENT = 0.8f;
        private const int MAIN_SYSTEM_AMOUNT = 2; 
        public event Action OnValueChanged;
        public event Action OnDestroy;
        
        [SerializeField] 
        [Range(0f, 1f)]
        private float dexterity;//why this is private
        protected float _shieldsBaseValue;
        private HealthModelDependency _healthModelDependency;

        [field:SerializeField] public float Armor { get; private set; }
        [field:SerializeField] public float Shields { get; private set; }
        
        [field:SerializeField] public float ShieldRegenerateValue { get; private set; }
        [field:SerializeField] public float ShieldRegenerateDelay { get; private set; }
        [field:SerializeField] public FloatRange ShieldDangerStateRange { get; private set; }
     

        public HardPointModel[] HardPointModels { get; private set; }

        [Inject]
        private DamageCalculationModel DamageCalculationModel { get; }
        
        
        public bool IsDestroyed { get; private set; }
        public bool HasShields => Shields > 0;
        public float Dexterity => dexterity;
        public float ShieldPercentage => Shields/ _shieldsBaseValue;

        public bool IsLostShieldGenerator { get; private set; }
        
        public bool HasUnits => HardPointModels.Any(x => !x.IsDestroyed);
        
        [Inject]
        public PlayerType PlayerType { get; }
        
        [Inject(Id = EntityBindType.ViewTransform)]
        public LazyInject<Transform> ViewTransform { get; }
        
        public Transform Transform => ViewTransform.Value;

        
        public IHardPointModel[] GetShipUnits(HardPointType hardPointType)
        {
            var currentHardPoints = HardPointModels.Where(x => !x.IsDestroyed).ToArray();

            if (currentHardPoints.Length == 0)
            {
                return null;
            }
            if (hardPointType == HardPointType.Any)
            {
                return currentHardPoints;
            }

            if (currentHardPoints.Any(x => x.HardPointType == hardPointType))
            {
                return currentHardPoints.Where(x => x.HardPointType == hardPointType).ToArray();
            }

            return currentHardPoints;
        }
        
        
        public void InjectDependency(HealthModelDependency healthModelDependency)
        {
            _healthModelDependency = healthModelDependency;
            HardPointModels = new HardPointModel[_healthModelDependency.ShipUnits.Count];
            for (var i = 0; i < _healthModelDependency.ShipUnits.Count; i++)
            {
                HardPointModels[i] = new HardPointModel(); // Initialize each element
                HardPointModels[i].SetData(_healthModelDependency.ShipUnits[i]);
            }

            float mainSystemCount = HardPointModels.Count(x=> x.HardPointType == HardPointType.Engines || x.HardPointType == HardPointType.ShieldGenerator);

            if (mainSystemCount == 0)
            {
                float health = Armor / HardPointModels.Length;
                foreach (HardPointModel shipUnitModel in HardPointModels)
                {
                    shipUnitModel.SetHealth(health);
                }
            }
            else
            {
                float weaponHealth = Armor * WEAPON_SYSTEM_COEFFICIENT;
                float weaponHealthPerUnit = weaponHealth / (HardPointModels.Length - mainSystemCount);
                float mainSystemHealth = Armor - weaponHealth;
                float mainSystemHealthPerUnit = mainSystemHealth / mainSystemCount;// assert 0 division
                
                foreach (HardPointModel shipUnitModel in HardPointModels)
                {
                    if (shipUnitModel.HardPointType == HardPointType.Engines ||
                        shipUnitModel.HardPointType == HardPointType.ShieldGenerator)
                    {
                        shipUnitModel.SetHealth(mainSystemHealthPerUnit);
                    }
                    else
                    {
                        shipUnitModel.SetHealth(weaponHealthPerUnit);
                    }
                }
            }
        }

    

        protected override void OnInit()
        {
            _shieldsBaseValue = Shields;
        }
        
        public void ApplyDamage(float damage, WeaponType weaponType, bool isMoving, int shipUnitId)
        {
            if(IsDestroyed) return;
            
            DamageData damageData = DamageCalculationModel.GetDamage(weaponType, this, isMoving, damage);
            
            
            Shields -= damageData.ShieldDamage;
            Armor -= damageData.ArmorDamage;

            HardPointModel hardPointModel = HardPointModels[shipUnitId];
      

            float damageLeft = ApplyDamageOnShipUnit(hardPointModel, damageData.ArmorDamage);
            if (damageLeft > 0)
            {
                ApplyDamageOnAllUnit(damageLeft);
            }
            
                
            if (Armor <= 0)
            {
                IsDestroyed = true;
                
                OnDestroy?.Invoke();
            }
            
            OnValueChanged?.Invoke();
        }

        public void ApplyDamageOnAllUnit(float damage)
        {
            float damageLeft = damage;
            foreach (HardPointModel hardPointModel in HardPointModels)
            {
                if(hardPointModel.IsDestroyed) continue;
                
                damageLeft = ApplyDamageOnShipUnit(hardPointModel, damageLeft);
                if (damageLeft == 0)
                {
                    break;
                }
            }

            if (damageLeft > 0)
            {
                Debug.LogError("Not intended behaviour -> Damage left: " + damageLeft);
            }
        }

        private float ApplyDamageOnShipUnit(HardPointModel hardPointModel, float damage)
        {
            float damageLeft = hardPointModel.TryApplyDamage(damage);
            
            
            if (hardPointModel.HardPointType == HardPointType.ShieldGenerator && hardPointModel.IsDestroyed)
            {
                IsLostShieldGenerator = true;
                Shields = 0;
                // OnValueChanged?.Invoke();
            }

            return damageLeft;
        }

        public void RegenerateShields(float value)
        {
            Shields += value;
            OnValueChanged?.Invoke();
        }


#if UNITY_EDITOR
        public void SetHardPoints(IHardPointProvider[] providers)
        {
            HardPointModels = new HardPointModel[providers.Length];
            for (var i = 0; i < providers.Length; i++)
            {
                HardPointModels[i] = new HardPointModel(); // Initialize each element
                HardPointModels[i].SetDataFromEditor(providers[i].Id, providers[i].HardPointType);
            }
        }  
#endif
    }
}