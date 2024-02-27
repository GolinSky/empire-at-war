using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.Services.TimerPoolWrapperService;
using EmpireAtWar.ViewComponents.Health;
using LightWeightFramework.Model;
using UnityEngine;
using Utils.TimerService;
using WorkShop.LightWeightFramework.Components;
using Zenject;
using ICommand = WorkShop.LightWeightFramework.Command.ICommand;

namespace EmpireAtWar.Components.Ship.WeaponComponent
{
    public interface IWeaponComponent:IComponent
    {
        void AddTargets(AttackData[] healthComponent);
        void AddTarget(AttackData healthComponent, AttackType attackType);
        bool HasEnoughRange(float distance);
    }

    public enum AttackType
    {
        Base = 0,
        MainTarget = 1,
    }

    public interface IWeaponCommand:ICommand
    {
        void ApplyDamage(IShipUnitView unitView, WeaponType weaponType);
    }
    public class WeaponComponent : BaseComponent<WeaponModel>, IWeaponComponent, IWeaponCommand, ILateTickable
    {
        private readonly ITimerPoolWrapperService timerPoolWrapperService;
        private readonly IMoveModelObserver moveModelObserver;
        private readonly ITimer attackTimer;
        private float endTimeTween;

        private List<AttackData> attackDataList = new List<AttackData>();
        private AttackData mainAttackData = null;
        public WeaponComponent(
            IModel model,
            ITimerPoolWrapperService timerPoolWrapperService) : base(model)
        {
            this.timerPoolWrapperService = timerPoolWrapperService;
            moveModelObserver = model.GetModelObserver<IMoveModelObserver>();
            attackTimer = TimerFactory.ConstructTimer(3f);
        }

        public void AddTargets(AttackData[] attackDataArray)
        {
            foreach (AttackData component in attackDataArray)
            {
                AddTarget(component, AttackType.Base);
            }
        }

        public void AddTarget(AttackData attackData, AttackType attackType)
        {
            switch (attackType)
            {
                case AttackType.Base:
                {
                    foreach (AttackData data in attackDataList)
                    {
                        if (attackData == data)
                        {
                            return;
                        }
                    }
                    Model.AddShipUnits(attackData.Units);
                    attackDataList.Add(attackData);
                    break;
                }
                case AttackType.MainTarget:
                {
                    mainAttackData = attackData;
                    Model.MainUnitsTarget = mainAttackData.Units;
                    break;
                }
            }
        }

        public bool HasEnoughRange(float distance)
        {
            return Model.MaxAttackDistance > distance;
        }

        public void ApplyDamage(IShipUnitView unitView, WeaponType weaponType)
        {
            for (var i = 0; i < attackDataList.Count; i++)
            {
                if (attackDataList[i].Contains(unitView))
                {
                    int index = i;
                    timerPoolWrapperService.Invoke(
                        ()=>
                        {   
                            if(index > attackDataList.Count - 1) return;
                            
                            ApplyDamageInternal(attackDataList[index], weaponType, unitView.Id,
                                GetDistance(unitView.Position));
                        },
                        Model.ProjectileDuration);
                    break;
                }
            }
        }

        private void ApplyDamageInternal(AttackData attackData, WeaponType weaponType, int id, float distance)
        {
            if (attackData.IsDestroyed)
            {
                RemoveAttackData(attackData);
                return;
            }
            attackData.ApplyDamage(Model.GetDamage(weaponType,distance), weaponType, id);
        }

        private void CheckAttackData(AttackData attackData)
        {
            if (attackData.IsDestroyed)
            {
                RemoveAttackData(attackData);
                return;
            }

            bool hasTargets = attackData.Units.Any(x => !x.IsDestroyed);

            if (!hasTargets)
            {
                Model.RemoveShipUnits(attackData.Units);
                if (attackData.TryUpdateNewUnits())
                {
                    Model.AddShipUnits(attackData.Units);
                }
            }
        }

        private float GetDistance(Vector3 targetPosition) =>
            Vector3.Distance(moveModelObserver.CurrentPosition, targetPosition);

        private void RemoveAttackData(AttackData attackData)
        {
            attackDataList.Remove(attackData);
            Model.RemoveShipUnits(attackData.Units);
        }

        public void LateTick()
        {
            if(attackDataList.Count == 0) return;

            if (attackTimer.IsComplete)
            {
                attackTimer.StartTimer();

                if (mainAttackData is not null)
                {
                    if (mainAttackData.IsDestroyed)
                    {
                        mainAttackData = null;
                        Model.MainUnitsTarget = null;
                    }
         
                }
             
                
                for (var i = 0; i < attackDataList.Count; i++)
                {
                    CheckAttackData(attackDataList[i]);
                }
            }
        }
    }
}