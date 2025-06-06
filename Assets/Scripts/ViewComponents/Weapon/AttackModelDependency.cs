﻿using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public class AttackModelDependency: ModelDependency<IAttackModelObserver>
    {
        [SerializeField] private DictionaryWrapper<WeaponType, List<WeaponHardPointView>> turretDictionary;

        
        public Dictionary<WeaponType, List<WeaponHardPointView>> TurretDictionary => turretDictionary.Dictionary;   
        
        
        protected override void OnInit()
        {
            base.OnInit();
            Model.InjectDependency(this);// move it to the base class
        }
    }
}