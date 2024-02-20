using System.Collections.Generic;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.TimerPoolWrapperService;
using LightWeightFramework.Model;
using UnityEngine;
using Utils.TimerService;
using WorkShop.LightWeightFramework.Components;
using Zenject;

namespace EmpireAtWar.Components.Ship.WeaponComponent
{
    public interface IWeaponComponent:IComponent
    {
        void AddTarget(AttackData[] healthComponent);
    }
 
    public class WeaponComponent : BaseComponent<WeaponModel>, IInitializable, ILateDisposable, IWeaponComponent, ITickable
    {
        private readonly IBattleService battleService;
        private readonly ITimerPoolWrapperService timerPoolWrapperService;
        private readonly ISelectionModelObserver selectionModelObserver;
        private readonly IMoveModelObserver moveModelObserver;
        private readonly ITimer attackTimer;
        private float endTimeTween;
        private int index = -1;

        private List<AttackData> attackDataList = new List<AttackData>();
        
        public WeaponComponent(
            IModel model,
            IBattleService battleService,
            ITimerPoolWrapperService timerPoolWrapperService) : base(model)
        {
            this.battleService = battleService;
            this.timerPoolWrapperService = timerPoolWrapperService;
            selectionModelObserver = model.GetModelObserver<ISelectionModelObserver>();
            moveModelObserver = model.GetModelObserver<IMoveModelObserver>();
            attackTimer = TimerFactory.ConstructTimer(2f);
        }

        public void Initialize()
        {
      //      battleService.OnTargetAdded += OnTargetAdded;
        }

        public void LateDispose()
        {
         //   battleService.OnTargetAdded -= OnTargetAdded;
        }

        // private void OnTargetAdded(IHealthComponent healthComponent)
        // {
        //     if (selectionModelObserver.IsSelected)
        //     { 
        //         AddTargetInternal(healthComponent);
        //     }
        // }

        public void AddTarget(AttackData[] attackDataArray)
        {
            foreach (AttackData component in attackDataArray)
            {
                AddTargetInternal(component);
            }
        }

        private void AddTargetInternal(AttackData attackData)
        {
            foreach (AttackData data in attackDataList)
            {
                if (attackData == data)
                {
                    data.UpdateData(attackData);
                    return;
                }
            }
            
            attackDataList.Add(attackData);
        }

        public void ApplyDamage(AttackData attackData, WeaponType weaponType, float distance)
        {
            timerPoolWrapperService.Invoke(
                ()=> ApplyDamageInternal(attackData, weaponType, distance),
                Model.ProjectileDuration);
        }

        private void ApplyDamageInternal(AttackData attackData, WeaponType weaponType, float distance)
        {
            attackData.ApplyDamage(Model.GetDamage(weaponType,distance), weaponType);
        }

        private void Attack(AttackData attackData)
        {
            if (attackData.IsDestroyed)
            {
                attackDataList.Remove(attackData);
                return;
            }
            float distance = GetDistance(attackData.Position);
            if (distance > Model.MaxAttackDistance)
            {
                attackTimer.ForceFinish();
                return;
            }
            
            Model.UpdateAttackData(attackData.Position, Model.Filter(distance), ((weaponType, distance) =>
            {
                ApplyDamage(attackData, weaponType, distance);
            }));
        }

        public void Tick()
        {
            if(attackDataList.Count == 0) return;

            if (attackTimer.IsComplete)
            {
                for (var i = 0; i < attackDataList.Count; i++)
                {
                    Attack(attackDataList[i]);
                }
            
                attackTimer.StartTimer();
            }
        }

        private float GetDistance(Vector3 targetPosition) =>
            Vector3.Distance(moveModelObserver.CurrentPosition, targetPosition);
        
    }
}