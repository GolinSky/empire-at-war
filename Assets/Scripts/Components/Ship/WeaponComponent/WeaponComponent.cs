using DG.Tweening;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.Services.Battle;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Components.Ship.WeaponComponent
{
    public class WeaponComponent : BaseComponent<WeaponModel>, IInitializable, ILateDisposable
    {
        private readonly IBattleService battleService;
        private readonly ISelectionModelObserver selectionModelObserver;
        private readonly IMoveModelObserver moveModelObserver;
        
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
                float distance = Vector3.Distance(moveModelObserver.Position, healthComponent.Position);
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
    }
}