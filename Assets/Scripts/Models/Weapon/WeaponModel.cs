using System;
using System.Collections.Generic;
using EmpireAtWar.ScriptUtils.EditorSerialization;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Models.Weapon
{
    public interface IWeaponModelObserver : IModelObserver
    {
        event Action<Vector3> OnAttack;

        Dictionary<WeaponType, int> WeaponDictionary { get; }
        IProjectileModel ProjectileModel { get; }
        
        float ProjectileSpeed { get;}
        
        float MaxAttackDistance { get; }
    }

    [Serializable]
    public class WeaponModel : InnerModel, IWeaponModelObserver
    {
        public event Action<Vector3> OnAttack;

        [SerializeField] private DictionaryWrapper<WeaponType, int> weaponCount;
        
        [field:SerializeField] public float ProjectileSpeed { get; private set; }

        public Dictionary<WeaponType, int> WeaponDictionary => weaponCount.Dictionary;
        
        public IProjectileModel ProjectileModel { get; set; }
        
        [Inject]
        private WeaponDamageModel WeaponDamageModel { get; }

        public float MaxAttackDistance
        {
            get
            {
                float maxDistance = 0;

                foreach (WeaponType weaponType in WeaponDictionary.Keys)
                {
                    float distance = WeaponDamageModel.DamageDictionary[weaponType].Distance;
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                    }
                }

                return maxDistance;
            }
        }


        public Vector3 TargetPosition
        {
            set => OnAttack?.Invoke(value);
        }


        public float GetTotalDamage()
        {
            float damage = 0;
            foreach (var keyValuePair in WeaponDictionary)
            {
                damage += WeaponDamageModel.DamageDictionary[keyValuePair.Key].Damage * keyValuePair.Value;
            }
            return damage; 
        }
    }
}