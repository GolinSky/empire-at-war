using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Health;
using Utilities.ScriptUtils.EditorSerialization;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Weapon;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Models.Weapon
{
    public interface IWeaponModelObserver : IModelObserver
    {
        event Action OnMainUnitSwitched;
        Dictionary<WeaponType, int> WeaponDictionary { get; }
        IProjectileModel ProjectileModel { get; }
        
        float MaxAttackDistance { get; }
        List<IHardPointModel> Targets { get; }
        List<IHardPointModel> MainUnitsTarget { get; }
        float DelayBetweenAttack { get; }
        float GetAttackDistance(WeaponType weaponType);
        void InjectDependency(AttackModelDependency attackModelDependency);
    }

    [Serializable]
    public class WeaponModel : InnerModel, IWeaponModelObserver
    {
        public event Action OnMainUnitSwitched;

        [field: SerializeField] public float DelayBetweenAttack { get; set; }

        private List<IHardPointModel> _shipUnitViews = new List<IHardPointModel>();
        private List<IHardPointModel> _mainUnitsTarget;


        public Dictionary<WeaponType, int> WeaponDictionary { get; } = new Dictionary<WeaponType, int>();

        [Inject] public IProjectileModel ProjectileModel { get; }

        [Inject] private WeaponDamageModel WeaponDamageModel { get; }

        List<IHardPointModel> IWeaponModelObserver.Targets => _shipUnitViews;

        public List<IHardPointModel> MainUnitsTarget
        {
            get => _mainUnitsTarget;
            set
            {
                _mainUnitsTarget = value;
                OnMainUnitSwitched?.Invoke();
            }
        }

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
            foreach (KeyValuePair<WeaponType, int> keyValuePair in WeaponDictionary)
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

        public float GetAttackDistance(WeaponType weaponType)
        {
            return WeaponDamageModel.DamageDictionary[weaponType].Distance;
        }

        public void InjectDependency(AttackModelDependency attackModelDependency)
        {
            foreach (var keyValuePair in attackModelDependency.TurretDictionary)
            {
                WeaponDictionary.Add(keyValuePair.Key, keyValuePair.Value.Count);
            }
        }


        public void AddShipUnits(IEnumerable<IHardPointModel> units)
        {
            _shipUnitViews.AddRange(units);
        }

        public void RemoveShipUnits(IEnumerable<IHardPointModel> unitViews)
        {
            foreach (IHardPointModel shipUnitView in unitViews)
            {
                _shipUnitViews.Remove(shipUnitView);
            }
        }
        
        
        public float GetDamage(WeaponType weaponType, float distance)
        {
            return WeaponDamageModel.DamageDictionary[weaponType].GetDamage(distance);
        }
    }
}