using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.TimerPoolWrapperService;
using LightWeightFramework.Model;
using UnityEngine;
using Utils.TimerService;
using WorkShop.LightWeightFramework.Command;
using WorkShop.LightWeightFramework.Components;
using Zenject;

namespace EmpireAtWar.Components.Ship.WeaponComponent
{
    public interface IWeaponComponent:IComponent
    {
        void AddTarget(IHealthComponent[] healthComponent);

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

        private List<IHealthComponent> healthComponents = new List<IHealthComponent>();
        
        public WeaponComponent(
            IModel model,
            IBattleService battleService,
            ITimerPoolWrapperService timerPoolWrapperService) : base(model)
        {
            this.battleService = battleService;
            this.timerPoolWrapperService = timerPoolWrapperService;
            selectionModelObserver = model.GetModelObserver<ISelectionModelObserver>();
            moveModelObserver = model.GetModelObserver<IMoveModelObserver>();
            attackTimer = TimerFactory.ConstructTimer(1f);
        }

        public void Initialize()
        {
            battleService.OnTargetAdded += OnTargetAdded;
        }

        public void LateDispose()
        {
            battleService.OnTargetAdded -= OnTargetAdded;
        }

        private void OnTargetAdded(IHealthComponent healthComponent)
        {
            if (selectionModelObserver.IsSelected)
            { 
                AddTargetInternal(healthComponent);
            }
        }

        public void AddTarget(IHealthComponent[] healthComponent)
        {
            foreach (IHealthComponent component in healthComponent)
            {
                AddTargetInternal(component);
            }
        }

        private void AddTargetInternal(IHealthComponent healthComponent)
        {
            if (!healthComponents.Contains(healthComponent))
            {
                healthComponents.Add(healthComponent);
            }
        }

        public void ApplyDamage(IHealthComponent healthComponent, WeaponType weaponType, float distance)
        {
            timerPoolWrapperService.Invoke(
                ()=> ApplyDamageInternal(healthComponent, weaponType, distance),
                Model.ProjectileDuration);
        }

        private void ApplyDamageInternal(IHealthComponent healthComponent, WeaponType weaponType, float distance)
        {
            healthComponent.ApplyDamage(Model.GetDamage(weaponType,distance), weaponType);
        }

        private void Attack(IHealthComponent healthComponent)
        {
            if (healthComponent == null || healthComponent.Destroyed)
            {
                healthComponents.Remove(healthComponent);
                return;
            }

            float distance = GetDistance(healthComponent.Position);
            if (distance > Model.MaxAttackDistance)
            {
                attackTimer.ForceFinish();
                return;
            }

            Model.UpdateAttackData(healthComponent.Position, Model.Filter(distance), ((weaponType, distance) =>
            {
                ApplyDamage(healthComponent, weaponType, distance);
            }));
        }

        public void Tick()
        {
            if(healthComponents.Count == 0) return;

            if (attackTimer.IsComplete)
            {
                foreach (IHealthComponent healthComponent in healthComponents)
                {
                    Attack(healthComponent);
                }
                attackTimer.StartTimer();
            }
        }

        private float GetDistance(Vector3 targetPosition) =>
            Vector3.Distance(moveModelObserver.CurrentPosition, targetPosition);
        
    }
}