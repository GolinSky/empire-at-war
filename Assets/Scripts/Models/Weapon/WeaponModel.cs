using System;
using System.Collections.Generic;
using EmpireAtWar.ScriptUtils.EditorSerialization;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Weapon
{
    public interface IWeaponModelObserver : IModelObserver
    {
        Dictionary<WeaponType, int> WeaponCount { get; }
    }

    [Serializable]
    public class WeaponModel : InnerModel, IWeaponModelObserver
    {
        [SerializeField] private DictionaryWrapper<WeaponType, int> weaponCount;

        public Dictionary<WeaponType, int> WeaponCount => weaponCount.Dictionary;

        public float GetTotalDamage()
        {
            return 3000; //hardcode
        }
    }
}