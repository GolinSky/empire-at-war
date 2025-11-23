using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.ViewComponents.Health;
using LightWeightFramework.Components.Components;
using UnityEngine;
using Utilities.ScriptUtils.EditorSerialization;
using Zenject;

namespace EmpireAtWar.Components.Weapon
{
    public interface IWeaponComponent: IComponent
    {
        void AddTarget(AttackData attackData, AttackType attackType);
        bool HasEnoughRange(float distance);
        void ResetTarget();
    }
    
    public class WeaponComponent: MonoComponent<WeaponModel>, IWeaponComponent, ITickable
    {
        [SerializeField] private List<WeaponHardPointView> hardPoints;
        
        private List<AttackData> _attackDataList = new List<AttackData>();
        private AttackData _mainAttackData = null;
        
        public void AddTarget(AttackData attackData, AttackType attackType)
        {
            switch (attackType)
            {
                case AttackType.Base:
                {
                     _attackDataList.Add(attackData);
                    break;
                }
                case AttackType.MainTarget:
                {
                    _mainAttackData = attackData;
                    _attackDataList.Add(attackData);
                    break;
                }
            }
        }

        public bool HasEnoughRange(float distance)
        {
            return Model.OptimalAttackRange > distance;
        }

        public void ResetTarget()
        {
            _mainAttackData = null;
        }

        public void Tick()
        {
            if (_mainAttackData != null)
            {
                if (_mainAttackData.IsDestroyed)
                {
                    ResetTarget();
                }
                else
                {
                    foreach (WeaponHardPointView weapon in hardPoints)
                    {
                        if(weapon.IsDestroyed || weapon.IsBusy) continue;
            
                        foreach (IHardPointModel hardPointModel in _mainAttackData.Units)
                        {
                            if (weapon.CanAttack(hardPointModel.Position))
                            {
                                weapon.Attack(hardPointModel);
                            }
                        }
                    }
                }
            }
     
            
            for (var i = _attackDataList.Count - 1; i >= 0; i--)
            {
                AttackData attackData = _attackDataList[i];
                if (attackData.IsDestroyed)
                {
                    _attackDataList.Remove(attackData);
                    continue;
                }

                bool hasTargets = attackData.Units.Any(x => !x.IsDestroyed);

                if (!hasTargets)
                {
                    if (!attackData.TryUpdateNewUnits())
                    {
                       _attackDataList.Remove(attackData);
                       continue;
                    }
                }
                
                foreach (WeaponHardPointView hardPoint in hardPoints)
                {
                    if(hardPoint.IsDestroyed || hardPoint.IsBusy) continue;
                    
                    foreach (IHardPointModel hardPointModel in attackData.Units)
                    {
                        if (hardPoint.CanAttack(hardPointModel.Position))
                        {
                            hardPoint.Attack(hardPointModel);
                        }
                    }
                }
            }
        }
    }
}