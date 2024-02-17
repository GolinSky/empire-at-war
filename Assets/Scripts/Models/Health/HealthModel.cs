using System;
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

        float Armor { get; }
        float Shields { get; }
    }

    [Serializable]
    public class HealthModel:InnerModel, IHealthModelObserver, IHealthData
    {
        public event Action OnValueChanged;
        public event Action OnDestroy;
        
        [SerializeField] 
        [Range(0f, 1f)]
        private float dexterity;

        [field:SerializeField] public float Armor { get; private set; }
        [field:SerializeField] public float Shields { get; private set; }
        
        [Inject]
        private DamageCalculationModel DamageCalculationModel { get; }
        public bool IsDestroyed { get; private set; }
        

        public bool HasShields => Shields > 0;
        public float Dexterity => dexterity;

        public void ApplyDamage(float damage, WeaponType weaponType, bool isMoving)
        {
            DamageData damageData = DamageCalculationModel.GetDamage(weaponType, this, isMoving, damage);
            
            Debug.Log($"ApplyDamage: {damage} -> [ShieldDamage: {damageData.ShieldDamage}], [ArmorDamage: {damageData.ArmorDamage}];");
            Shields -= damageData.ShieldDamage;
            Armor -= damageData.ArmorDamage;
            if (Armor <= 0)
            {
                IsDestroyed = true;
                OnDestroy?.Invoke();
            }
            OnValueChanged?.Invoke();
        }
    }
}