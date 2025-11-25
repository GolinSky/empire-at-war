using System.Collections.Generic;
using LightWeightFramework.Model;
using UnityEngine;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Components.AttackComponent
{
    public interface IProjectileModel:IModelObserver
    {
        ProjectileData GetData(WeaponType weaponType);
    }
    
    [CreateAssetMenu(fileName = "ProjectileModel", menuName = "Model/Weapon/ProjectileModel")]
    public class ProjectileModel:Model, IProjectileModel
    {
        [SerializeField] private DictionaryWrapper<WeaponType, ProjectileData> projectileData;

        private Dictionary<WeaponType, ProjectileData> ProjectileData => projectileData.Dictionary;

        public ProjectileData GetData(WeaponType weaponType)
        {
            return ProjectileData[weaponType];
        }
    }
}