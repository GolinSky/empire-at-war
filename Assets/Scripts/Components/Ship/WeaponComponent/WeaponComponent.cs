using System.Collections.Generic;
using System.Windows.Input;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.Services.Battle;
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
        void AddTarget(AttackData[] healthComponent);
    }


    public interface IWeaponCommand:ICommand
    {
        void ApplyDamage(IShipUnitView unitView, WeaponType weaponType);
    }
    public class WeaponComponent : BaseComponent<WeaponModel>, IInitializable, ILateDisposable, IWeaponComponent, ITickable, IWeaponCommand
    {
        private readonly IBattleService battleService;
        private readonly ITimerPoolWrapperService timerPoolWrapperService;
        private readonly ISelectionModelObserver selectionModelObserver;
        private readonly IMoveModelObserver moveModelObserver;
        private readonly ITimer attackTimer;
        private float endTimeTween;
        private float attackDelay;

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
            attackDelay = Model.WeaponCount * Model.DelayBetweenAttack + 1f;
            attackTimer = TimerFactory.ConstructTimer(10f);
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
                    Model.Targets.Remove(data.ShipUnitView);
                    data.UpdateData(attackData);
                    Model.Targets.Add(data.ShipUnitView);
                    return;
                }
            }
            Model.Targets.Add(attackData.ShipUnitView);
            attackDataList.Add(attackData);
        }

        public void ApplyDamage(IShipUnitView unitView, WeaponType weaponType)
        {
            for (var i = 0; i < attackDataList.Count; i++)
            {
                if (attackDataList[i].ShipUnitView == unitView)
                {
                    ApplyDamage(attackDataList[i], weaponType, GetDistance(unitView.Position));
                    break;
                }
            }
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
                RemoveAttackData(attackData);
                return;
            }
            float distance = GetDistance(attackData.Position);
            if (distance > Model.MaxAttackDistance)
            {
              //  attackTimer.ForceFinish();
                RemoveAttackData(attackData);
            }
        }

        public void Tick()
        {
            if(attackDataList.Count == 0) return;

            for (var i = 0; i < attackDataList.Count; i++)
            {
                Attack(attackDataList[i]);
            }
           
        }

        private float GetDistance(Vector3 targetPosition) =>
            Vector3.Distance(moveModelObserver.CurrentPosition, targetPosition);


        private void RemoveAttackData(AttackData attackData)
        {
            attackDataList.Remove(attackData);
            Model.Targets.Remove(attackData.ShipUnitView);
        }

        public bool TryGetCommand<TCommand>(out TCommand command) where TCommand : ICommand
        {
            throw new System.NotImplementedException();
        }

     
    }
}