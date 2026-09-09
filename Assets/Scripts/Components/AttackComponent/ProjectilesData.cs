using System.Collections.Generic;
using EmpireAtWar.Mvc;
using UnityEngine;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Components.AttackComponent
{
    public interface IProjectileModel:IModelObserver
    {
        ProjectileData GetData(WeaponType weaponType);
    }
    
    [CreateAssetMenu(fileName = nameof(ProjectilesData), menuName = "Data/Weapon/ProjectilesData")]
    public class ProjectilesData:Data, IProjectileModel
    {
        [SerializeField] private DictionaryWrapper<WeaponType, ProjectileData> projectileData;

        private Dictionary<WeaponType, ProjectileData> ProjectileData => projectileData.Dictionary;

        public ProjectileData GetData(WeaponType weaponType)
        {
            return ProjectileData[weaponType];
        }
    }
}