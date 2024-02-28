using System;
using System.Linq;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Weapon;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Models.Health
{
    public interface IHealthModelObserver:IModelObserver
    {
        event Action OnDestroy;
        event Action OnValueChanged;

        ShipUnitModel[] ShipUnitModels { get; }
        float Armor { get; }
        float Shields { get; }
        float ShieldPercentage { get; }
        bool IsDestroyed { get; }
        bool IsLostShieldGenerator { get; }

    }

    [Serializable]
    public class HealthModel:InnerModel, IHealthModelObserver, IHealthData
    {
        private const float MainSystemCoefficient = 0.1f;
        private const float WeaponSystemCoefficient = 0.8f;
        private const int MainSystemAmount = 2; 
        public event Action OnValueChanged;
        public event Action OnDestroy;
        
        [SerializeField] 
        [Range(0f, 1f)]
        private float dexterity;

        [field:SerializeField] public float Armor { get; private set; }
        [field:SerializeField] public float Shields { get; private set; }
        
        [field:SerializeField] public float ShieldRegenerateValue { get; private set; }
        [field:SerializeField] public float ShieldRegenerateDelay { get; private set; }
        [field:SerializeField] public ShipUnitModel[] ShipUnitModels { get; private set; }

        protected float shieldsBaseValue;
        [Inject]
        private DamageCalculationModel DamageCalculationModel { get; }
        public bool IsDestroyed { get; private set; }
        public bool HasShields => Shields > 0;
        public float Dexterity => dexterity;
        public float ShieldPercentage => Shields/ shieldsBaseValue;

        public bool IsLostShieldGenerator { get; private set; }
        
        protected override void OnInit()
        {
            shieldsBaseValue = Shields;
            float health = (Armor*WeaponSystemCoefficient) / (ShipUnitModels.Length - MainSystemAmount);

            foreach (ShipUnitModel shipUnitModel in ShipUnitModels)
            {
                if (shipUnitModel.ShipUnitType == ShipUnitType.Engines ||
                    shipUnitModel.ShipUnitType == ShipUnitType.ShieldGenerator)
                {
                    shipUnitModel.SetHealth(Armor*MainSystemCoefficient);
                }
                else
                {
                    shipUnitModel.SetHealth(health);
                }
            }
        }
        
        public void ApplyDamage(float damage, WeaponType weaponType, bool isMoving, int shipUnitId)
        {
            DamageData damageData = DamageCalculationModel.GetDamage(weaponType, this, isMoving, damage);
            
            Shields -= damageData.ShieldDamage;
            Armor -= damageData.ArmorDamage;
                //todo : why here out of bounds 
            ShipUnitModel shipUnitModel = ShipUnitModels[shipUnitId];
            if (damageData.ArmorDamage > shipUnitModel.Health)
            {
                float damageLeft = damageData.ArmorDamage - shipUnitModel.Health;
                ApplyDamageOnShipUnit(shipUnitModel, shipUnitModel.Health);
                ApplyDamageOnAllUnit(damageLeft);
            }
            else
            {
                ApplyDamageOnShipUnit(shipUnitModel, damageData.ArmorDamage);
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
            ShipUnitModel[] unitModels = ShipUnitModels.Where(x => x.Health > 0).ToArray();
            float damagePerUnit = damage / unitModels.Length;
            foreach (ShipUnitModel shipUnitModel in unitModels)
            {
                ApplyDamageOnShipUnit(shipUnitModel, damagePerUnit);
            }
        }

        private void ApplyDamageOnShipUnit(ShipUnitModel shipUnitModel, float damage)
        {
            shipUnitModel.ApplyDamage(damage);
            if (shipUnitModel.ShipUnitType == ShipUnitType.ShieldGenerator && shipUnitModel.Health <= 0f)
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
    }
}