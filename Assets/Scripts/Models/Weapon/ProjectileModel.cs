using System.Collections.Generic;
using EmpireAtWar.ScriptUtils.EditorSerialization;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Weapon
{
    public interface IProjectileModel:IModelObserver
    {
        Dictionary<WeaponType, ProjectileData> ProjectileData { get; }
    }
    
    [CreateAssetMenu(fileName = "ProjectileModel", menuName = "Model/Weapon/ProjectileModel")]
    public class ProjectileModel:Model, IProjectileModel
    {
        [SerializeField] private DictionaryWrapper<WeaponType, ProjectileData> projectileData;
        
        public Dictionary<WeaponType, ProjectileData> ProjectileData => projectileData.Dictionary;
    }
}