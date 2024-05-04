using System;
using EmpireAtWar.Models.Weapon;
using Utilities.ScriptUtils.EditorSerialization;
using LightWeightFramework.Model;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EmpireAtWar.Models.Health
{
    [CreateAssetMenu(fileName = "DamageCalculationModel", menuName = "Model/DamageCalculationModel")]
    public class DamageCalculationModel:Model
    {
        [SerializeField] private DictionaryWrapper<WeaponType, DamageModel> damageWrapper;
        
        public DamageData GetDamage(WeaponType weaponType, IHealthData healthData, bool isMoving, float damage)
        {
            return damageWrapper.Dictionary[weaponType].GetDamage(healthData,isMoving, damage);
        }
    }

    [Serializable]
    public class DamageModel
    {
        [Range(0,1)][SerializeField] private float damageOnShieldCoefficient;
        [Range(0,1)][SerializeField] private float damageOnArmorCoefficient;
        [Range(0,1)][SerializeField] private float minAccuracyCoefficient;
        [Range(0,1)][SerializeField] private float maxAccuracyCoefficient;
        [Range(0,1)][SerializeField] private float shieldPenetrationCoefficient;

        private float AccuracyCoefficient => Random.Range(minAccuracyCoefficient, maxAccuracyCoefficient);
        
        public DamageData GetDamage(IHealthData healthData, bool isMoving, float damage)
        {
            float damageOnShield = 0f;
            float damageOnArmor = 0f;
            
            if (healthData.HasShields)
            {
                damageOnShield = GetCalculatedDamage(healthData, isMoving, damage);

                if (healthData.Shields > damageOnShield)
                {
                    damageOnArmor = damageOnShield * shieldPenetrationCoefficient;
                }
                else
                {
                    damageOnArmor = healthData.Shields;
                }
                
                damageOnShield -= damageOnArmor;
            }
            else
            {
                damageOnArmor = GetCalculatedDamage(healthData, isMoving, damage);
            }


            return new DamageData(damageOnShield, damageOnArmor);
        }

        private float GetCalculatedDamage(IHealthData healthData, bool isMoving, float damage)
        {
            float calculatedDamage = damage;
            calculatedDamage *= isMoving
                ? (AccuracyCoefficient - healthData.Dexterity)
                : AccuracyCoefficient;
            
            calculatedDamage *= healthData.HasShields
                ? damageOnShieldCoefficient 
                : damageOnArmorCoefficient;
            return calculatedDamage;
        }
    }

    public struct DamageData
    {
        public readonly float ShieldDamage { get; }
        public readonly float ArmorDamage { get; }
        
        public DamageData(float shieldDamage, float armorDamage)
        {
            ShieldDamage = shieldDamage;
            ArmorDamage = armorDamage;
        }
    }
}