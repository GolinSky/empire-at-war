﻿using System;
using EmpireAtWar.Components.AttackComponent;
using Utilities.ScriptUtils.EditorSerialization;
using LightWeightFramework.Model;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EmpireAtWar.Models.Health
{
    //todo: make decorator here for health model
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
        private const float MIN_DEXTERITY_COEFFICIENT = 0.3f;
        private const float MAX_DEXTERITY_COEFFICIENT = 0.6f;
        
        [Range(0,1)][SerializeField] private float damageOnShieldCoefficient;
        [Range(0,1)][SerializeField] private float damageOnArmorCoefficient;
        [Range(0,1)][SerializeField] private float minAccuracyCoefficient;
        [Range(0,1)][SerializeField] private float maxAccuracyCoefficient;
        [Range(0,1)][SerializeField] private float shieldPenetrationCoefficient;

        private float AccuracyCoefficient => Random.Range(minAccuracyCoefficient, maxAccuracyCoefficient);
        
        private float DexterityCoefficient => Random.Range(MIN_DEXTERITY_COEFFICIENT, MAX_DEXTERITY_COEFFICIENT);
        
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

            Debug.Assert(damageOnShield >= 0, $"{nameof(damageOnShield)} cannot be smaller than 0.");
            Debug.Assert(damageOnArmor >= 0, $"{nameof(damageOnArmor)} cannot be smaller than 0.");
            
            return new DamageData(damageOnShield, damageOnArmor);
        }

        private float GetCalculatedDamage(IHealthData healthData, bool isMoving, float damage)
        {
            float calculatedDamage = damage;
            
            calculatedDamage *= AccuracyCoefficient;

            if (isMoving)
            {
                calculatedDamage -= (calculatedDamage*DexterityCoefficient) * healthData.Dexterity; 
            }
          
            
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