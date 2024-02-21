using System;
using System.Collections.Generic;
using EmpireAtWar.ScriptUtils.EditorSerialization;
using EmpireAtWar.ViewComponents.Health;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Models.Weapon
{
    public interface IWeaponModelObserver : IModelObserver
    {
        Dictionary<WeaponType, int> WeaponDictionary { get; }
        IProjectileModel ProjectileModel { get; }
        
        float ProjectileDuration { get;}
        
        float MaxAttackDistance { get; }
        
        float DelayBetweenAttack { get; set; }
        List<IShipUnitView> Targets { get; }
        List<WeaponType> Filter(float distance);
    }

    [Serializable]
    public class WeaponModel : InnerModel, IWeaponModelObserver
    {

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
            Targets = new List<IShipUnitView>();
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
        
        public List<IShipUnitView> Targets { get; set; }
        

        public float GetDamage(WeaponType weaponType, float distance)
        {
            return WeaponDamageModel.DamageDictionary[weaponType].GetDamage(distance);
        }
    }
}