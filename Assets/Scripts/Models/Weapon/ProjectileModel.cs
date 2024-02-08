using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Skirmish;
using EmpireAtWar.ScriptUtils.EditorSerialization;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;

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

        [Inject] public SkirmishGameData SkirmishGameData;
        public Dictionary<WeaponType, ProjectileData> ProjectileData => projectileData.Dictionary;

      
    }
}