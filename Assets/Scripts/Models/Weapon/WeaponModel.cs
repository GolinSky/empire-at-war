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
        event Action<Vector3, List<WeaponType>, Action<WeaponType, float>> OnAttack;
        Dictionary<WeaponType, int> WeaponDictionary { get; }
        IProjectileModel ProjectileModel { get; }
        
        float ProjectileDuration { get;}
        
        float MaxAttackDistance { get; }
        
        float DelayBetweenAttack { get; set; }
    }

    [Serializable]
    public class WeaponModel : InnerModel, IWeaponModelObserver
    {
        public event Action<Vector3, List<WeaponType>, Action<WeaponType, float>> OnAttack;

        [SerializeField] private DictionaryWrapper<WeaponType, int> weaponCount;
        
        [field:SerializeField] public float ProjectileDuration { get; private set; }

        [field:SerializeField] public float DelayBetweenAttack { get; set; }

        public Dictionary<WeaponType, int> WeaponDictionary => weaponCount.Dictionary;
        
        [Inject]
        public IProjectileModel ProjectileModel { get;  }
        
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

        public int WeaponCount { get; private set; }

        protected override void OnInit()
        {
            base.OnInit();
            foreach (KeyValuePair<WeaponType,int> keyValuePair in WeaponDictionary)
            {
                WeaponCount += keyValuePair.Value;
            }
        }

        public List<WeaponType> Filter(float distance)
        {
            List<WeaponType> filter = new List<WeaponType>();
            foreach (WeaponType weaponType in WeaponDictionary.Keys)
            {
                float damageDistance = WeaponDamageModel.DamageDictionary[weaponType].Distance;
                if (damageDistance >= distance)
                {
                    filter.Add(weaponType);
                }
            }

            return filter;
        }
        
        public void UpdateAttackData(Vector3 targetPosition, List<WeaponType> filter, Action<WeaponType, float> attackAction)
        {
            OnAttack?.Invoke(targetPosition, filter, attackAction);
        }

        public float GetDamage(WeaponType weaponType, float distance)
        {
            return WeaponDamageModel.DamageDictionary[weaponType].GetDamage(distance);
        }
    }
}