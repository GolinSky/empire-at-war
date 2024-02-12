using System.Collections.Generic;
using DG.Tweening;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.Services.Battle;
using LightWeightFramework.Model;
using UnityEngine;
using WorkShop.LightWeightFramework.Components;
using Zenject;

namespace EmpireAtWar.Components.Ship.WeaponComponent
{
    public interface IWeaponComponent:IComponent
    {
        void AddTarget(IHealthComponent healthComponent, Transform transform);
    }
    public class WeaponComponent : BaseComponent<WeaponModel>, IInitializable, ILateDisposable, IWeaponComponent
    {
        private readonly IBattleService battleService;
        private readonly ISelectionModelObserver selectionModelObserver;
        private readonly IMoveModelObserver moveModelObserver;

        private Dictionary<IHealthComponent, Transform> targets = new Dictionary<IHealthComponent, Transform>();
        public WeaponComponent(IModel model, IBattleService battleService, ProjectileModel projectileModel) : base(model)
        {
            this.battleService = battleService;
            selectionModelObserver = model.GetModelObserver<ISelectionModelObserver>();
            moveModelObserver = model.GetModelObserver<IMoveModelObserver>();
            Model.ProjectileModel = projectileModel;
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
                float distance = Vector3.Distance(moveModelObserver.CurrentPosition, healthComponent.Position);
                if (distance > Model.MaxAttackDistance)
                {
                    return;
                }
                Model.TargetPosition = healthComponent.Position;

                float baseTime = distance / Model.ProjectileSpeed;

                Sequence sequence = DOTween.Sequence();
                sequence.AppendInterval(baseTime);
                sequence.AppendCallback(() =>
                {
                    healthComponent.ApplyDamage(Model.GetTotalDamage());
                });
            }
        }

        public void AddTarget(IHealthComponent healthComponent, Transform transform)
        {
            if (!targets.ContainsKey(healthComponent))
            {
                targets.Add(healthComponent, transform);
            }
            
            
            float distance = Vector3.Distance(moveModelObserver.CurrentPosition, transform.position);
            if (distance > Model.MaxAttackDistance)
            {
                return;
            }
            Model.TargetPosition = transform.position;

            float baseTime = distance / Model.ProjectileSpeed;

            Sequence sequence = DOTween.Sequence();
            sequence.AppendInterval(baseTime);
            sequence.AppendCallback(() =>
            {
                healthComponent.ApplyDamage(Model.GetTotalDamage());
            });
        }
    }
}