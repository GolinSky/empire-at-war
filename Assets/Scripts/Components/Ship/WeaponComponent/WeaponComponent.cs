using System.Collections.Generic;
using DG.Tweening;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.Services.Battle;
using LightWeightFramework.Model;
using UnityEngine;
using Utils.TimerService;
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
        private const float Delay = 0.1f;
        private readonly IBattleService battleService;
        private readonly ISelectionModelObserver selectionModelObserver;
        private readonly IMoveModelObserver moveModelObserver;
        private readonly ITimer attackTimer;
        private Sequence sequence;
        private float endTimeTween;
        private int index = -1;
        

        private List<IHealthComponent> healthComponents = new List<IHealthComponent>();
        public WeaponComponent(IModel model, IBattleService battleService, ProjectileModel projectileModel) : base(model)
        {
            this.battleService = battleService;
            selectionModelObserver = model.GetModelObserver<ISelectionModelObserver>();
            moveModelObserver = model.GetModelObserver<IMoveModelObserver>();
            Model.ProjectileModel = projectileModel;
            attackTimer = TimerFactory.ConstructTimer(2f);
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
                if (!healthComponents.Contains(healthComponent))
                {
                    healthComponents.Add(healthComponent);
                }
            }
        }

        public void AddTarget(IHealthComponent[] healthComponent)
        {
            foreach (IHealthComponent component in healthComponent)
            {
                if (!healthComponents.Contains(component))
                {
                    healthComponents.Add(component);
                }
            }
        }

        private void Attack(IHealthComponent healthComponent)
        {
            if (healthComponent == null || healthComponent.Destroyed)
            {
                healthComponents.Remove(healthComponent);
                return;
            }
            float distance = Vector3.Distance(moveModelObserver.CurrentPosition, healthComponent.Position);
            if (distance > Model.MaxAttackDistance)
            {
                attackTimer.ForceFinish();
                return;
            }
            Model.TargetPosition = healthComponent.Position;
           
            sequence = DOTween.Sequence();
            sequence.AppendInterval(Model.ProjectileDuration+Delay);
            sequence.AppendCallback(() =>
            {
                healthComponent.ApplyDamage(Model.GetTotalDamage());
            });
        }

        public void Tick()
        {
            if(healthComponents.Count == 0) return;

            if (attackTimer.IsComplete)
            {
                if (sequence != null && sequence.IsActive())
                {
                    float difference = endTimeTween - Time.time;
                    difference += Delay;
                    attackTimer.AppendTime(difference);
                    return;
                }
                Attack(GetNext());
                attackTimer.StartTimer();
            }
        }



        private IHealthComponent GetNext()
        {
            index++;

            if (index >= healthComponents.Count)
            {
                index = 0;
            }

            return healthComponents[index];
        }
    }
}