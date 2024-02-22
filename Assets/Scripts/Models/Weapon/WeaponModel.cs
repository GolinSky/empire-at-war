using System;
using System.Collections.Generic;
using EmpireAtWar.ScriptUtils.EditorSerialization;
using EmpireAtWar.ViewComponents.Health;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;
using Random = System.Random;

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
        event Action<List<IShipUnitView>> OnTargetsUpdate;
        List<WeaponType> Filter(float distance);
    }

    [Serializable]
    public class WeaponModel : InnerModel, IWeaponModelObserver
    {

        [SerializeField] private DictionaryWrapper<WeaponType, int> weaponCount;
        
        [field:SerializeField] public float ProjectileDuration { get; private set; }

        [field:SerializeField] public float DelayBetweenAttack { get; set; }

        private Random random = new Random();
        
        public Dictionary<WeaponType, int> WeaponDictionary => weaponCount.Dictionary;
        
        [Inject]
        public IProjectileModel ProjectileModel { get;  }
        
        [Inject]
        private WeaponDamageModel WeaponDamageModel { get; }

        List<IShipUnitView> IWeaponModelObserver.Targets => shipUnitViews;
        public event Action<List<IShipUnitView>> OnTargetsUpdate;

        private List<IShipUnitView> shipUnitViews = new List<IShipUnitView>();
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
        

        public void AddShipUnits(IEnumerable<IShipUnitView> units)
        {
            shipUnitViews.AddRange(units);
        }

        public void RemoveShipUnits(IEnumerable<IShipUnitView> unitViews)
        {
            foreach (IShipUnitView shipUnitView in unitViews)
            {
                shipUnitViews.Remove(shipUnitView);
            }
        }
        
        
        public float GetDamage(WeaponType weaponType, float distance)
        {
            return WeaponDamageModel.DamageDictionary[weaponType].GetDamage(distance);
        }
    }
}