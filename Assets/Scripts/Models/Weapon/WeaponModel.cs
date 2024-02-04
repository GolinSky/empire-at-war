using System;
using System.Collections.Generic;
using EmpireAtWar.ScriptUtils.EditorSerialization;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Weapon
{
    public interface IWeaponModelObserver : IModelObserver
    {
        event Action<Vector3> OnAttack;

        Dictionary<WeaponType, int> WeaponCount { get; }
        IProjectileModel ProjectileModel { get; }
        
        float ProjectileSpeed { get;}
    }

    [Serializable]
    public class WeaponModel : InnerModel, IWeaponModelObserver
    {
        public event Action<Vector3> OnAttack;

        [SerializeField] private DictionaryWrapper<WeaponType, int> weaponCount;
        
        [field:SerializeField] public float ProjectileSpeed { get; private set; }
        
        public Dictionary<WeaponType, int> WeaponCount => weaponCount.Dictionary;
        
        public IProjectileModel ProjectileModel { get; set; }

        public Vector3 TargetPosition
        {
            set => OnAttack?.Invoke(value);
        }


        public float GetTotalDamage()
        {
            return 3000; //hardcode
        }
    }


}