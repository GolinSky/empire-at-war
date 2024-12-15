using System;
using System.Linq;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Weapon;
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
    }

    [Serializable]
    public class HealthModel:InnerModel, IHealthModelObserver, IHealthData
    {
        private const float MAIN_SYSTEM_COEFFICIENT = 0.1f;
        private const float WEAPON_SYSTEM_COEFFICIENT = 0.8f;
        private const int MAIN_SYSTEM_AMOUNT = 2; 
        public event Action OnValueChanged;
        public event Action OnDestroy;
        
        [SerializeField] 
        [Range(0f, 1f)]
        private float dexterity;//why this is private
        protected float shieldsBaseValue;
        private HealthModelDependency _healthModelDependency;

        [field:SerializeField] public float Armor { get; private set; }
        [field:SerializeField] public float Shields { get; private set; }
        
        [field:SerializeField] public float ShieldRegenerateValue { get; private set; }
        [field:SerializeField] public float ShieldRegenerateDelay { get; private set; }
        [field:SerializeField] public HardPointModel[] HardPointModels { get; private set; }
        [field:SerializeField] public FloatRange ShieldDangerStateRange { get; private set; }
     


        [Inject]
        private DamageCalculationModel DamageCalculationModel { get; }
        
   
        
        public bool IsDestroyed { get; private set; }
        public bool HasShields => Shields > 0;
        public float Dexterity => dexterity;
        public float ShieldPercentage => Shields/ shieldsBaseValue;

        public bool IsLostShieldGenerator { get; private set; }
        
        
        public void InjectDependency(HealthModelDependency healthModelDependency)
        {
            _healthModelDependency = healthModelDependency;
            HardPointModels = new HardPointModel[_healthModelDependency.ShipUnits.Count];
            for (var i = 0; i < _healthModelDependency.ShipUnits.Count; i++)
            {
                HardPointModels[i] = new HardPointModel(); // Initialize each element
                HardPointModels[i].SetData(_healthModelDependency.ShipUnits[i]);
            }

            if (HardPointModels.Length <= MAIN_SYSTEM_AMOUNT)
            {
                float health = Armor / HardPointModels.Length;
                foreach (HardPointModel shipUnitModel in HardPointModels)
                {
                    shipUnitModel.SetHealth(health);
                }
            }
            else
            {
                float health = (Armor * WEAPON_SYSTEM_COEFFICIENT) / (HardPointModels.Length - MAIN_SYSTEM_AMOUNT);

                foreach (HardPointModel shipUnitModel in HardPointModels)
                {
                    if (shipUnitModel.HardPointType == HardPointType.Engines ||
                        shipUnitModel.HardPointType == HardPointType.ShieldGenerator)
                    {
                        shipUnitModel.SetHealth(Armor * MAIN_SYSTEM_COEFFICIENT);
                    }
                    else
                    {
                        shipUnitModel.SetHealth(health);
                    }
                }
            }
        }
        
        protected override void OnInit()
        {
            shieldsBaseValue = Shields;
        }
        
        public void ApplyDamage(float damage, WeaponType weaponType, bool isMoving, int shipUnitId)
        {
            DamageData damageData = DamageCalculationModel.GetDamage(weaponType, this, isMoving, damage);
            
            Shields -= damageData.ShieldDamage;
            Armor -= damageData.ArmorDamage;
                //todo : why here out of bounds 
            HardPointModel hardPointModel = HardPointModels[shipUnitId];
            if (damageData.ArmorDamage > hardPointModel.Health)
            {
                float damageLeft = damageData.ArmorDamage - hardPointModel.Health;
                ApplyDamageOnShipUnit(hardPointModel, hardPointModel.Health);
                ApplyDamageOnAllUnit(damageLeft);
            }
            else
            {
                ApplyDamageOnShipUnit(hardPointModel, damageData.ArmorDamage);
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
            HardPointModel[] unitModels = HardPointModels.Where(x => x.Health > 0).ToArray();
            float damagePerUnit = damage / unitModels.Length;
            foreach (HardPointModel shipUnitModel in unitModels)
            {
                ApplyDamageOnShipUnit(shipUnitModel, damagePerUnit);
            }
        }

        private void ApplyDamageOnShipUnit(HardPointModel hardPointModel, float damage)
        {
            hardPointModel.ApplyDamage(damage);
            if (hardPointModel.HardPointType == HardPointType.ShieldGenerator && hardPointModel.Health <= 0f)
            {
                IsLostShieldGenerator = true;
                Shields = 0;
                OnValueChanged?.Invoke();
            }
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