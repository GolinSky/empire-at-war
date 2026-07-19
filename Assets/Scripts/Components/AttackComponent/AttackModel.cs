using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Components.AttackComponent
{
    public interface IAttackData
    {
        float DelayBetweenAttack { get; }
    }

    public interface IAttackModelObserver : IModelObserver
    {
        event Action OnMainUnitSwitched;
        Dictionary<WeaponType, int> WeaponDictionary { get; }
        IProjectileModel ProjectileModel { get; }
        
        float MaxAttackDistance { get; }
        List<IHardPointModel> Targets { get; }
        List<IHardPointModel> MainUnitsTarget { get; }
        
        float DelayBetweenAttack { get; }
        float GetAttackDistance(WeaponType weaponType);
        void InjectDependency(IReadOnlyDictionary<WeaponType, List<WeaponHardPointView>> turretDictionary);
    }

    [Serializable]
    public class AttackModel : PureModel, IAttackModelObserver
    {
        private const float OPTIMAL_DISTANCE_MODIFIER = 0.5f;
        public event Action OnMainUnitSwitched;

        [Inject] private IAttackData Data { get; }
        public float DelayBetweenAttack => Data.DelayBetweenAttack;

        private List<IHardPointModel> _shipUnitViews = new List<IHardPointModel>();
        private List<IHardPointModel> _mainUnitsTarget;


        public Dictionary<WeaponType, int> WeaponDictionary { get; } = new Dictionary<WeaponType, int>();

        [Inject] public IProjectileModel ProjectileModel { get; }

        [Inject] private WeaponDamageModel WeaponDamageModel { get; }

        public List<IHardPointModel> Targets => _shipUnitViews;

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
                    float distance = WeaponDamageModel.GetDamageModel(weaponType).Distance;
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                    }
                }

                return maxDistance;
            }
        }

        public int WeaponCount { get; private set; }
        public float OptimalAttackRange => MaxAttackDistance * OPTIMAL_DISTANCE_MODIFIER;

        public List<WeaponType> Filter(float distance)
        {
            List<WeaponType> filter = new List<WeaponType>();
            foreach (WeaponType weaponType in WeaponDictionary.Keys)
            {
                float damageDistance = WeaponDamageModel.GetDamageModel(weaponType).Distance;
                if (damageDistance >= distance)
                {
                    filter.Add(weaponType);
                }
            }

            return filter;
        }

        public float GetAttackDistance(WeaponType weaponType)
        {
            return WeaponDamageModel.GetDamageModel(weaponType).Distance;
        }

        public void InjectDependency(IReadOnlyDictionary<WeaponType, List<WeaponHardPointView>> turretDictionary)
        {
            WeaponCount = 0;
            foreach (var keyValuePair in turretDictionary)
            {
                WeaponDictionary.Add(keyValuePair.Key, keyValuePair.Value.Count);
                WeaponCount += keyValuePair.Value.Count;
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
            return WeaponDamageModel.GetDamageModel(weaponType).GetDamage(distance);
        }
    }
}
