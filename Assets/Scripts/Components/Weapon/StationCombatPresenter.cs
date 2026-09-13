using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Battle;
using UnityEngine.Rendering;
using Zenject;

namespace EmpireAtWar.Components.Weapon
{
    public sealed class StationCombatPresenter : IInitializable, ILateDisposable, IObserver<ISelectionSubject>
    {
        private readonly IWeaponComponent _weaponComponent;
        private readonly IRadarModelObserver _radarModel;
        private readonly IHealthModelObserver _healthModel;
        private readonly ISelectionModelObserver _selectionModel;
        private readonly ISelectionService _selectionService;
        private readonly IAttackDataFactory _attackDataFactory;
        private bool _isReleased;

        public StationCombatPresenter(
            IWeaponComponent weaponComponent,
            IRadarModelObserver radarModel,
            IHealthModelObserver healthModel,
            ISelectionModelObserver selectionModel,
            ISelectionService selectionService,
            IAttackDataFactory attackDataFactory)
        {
            _weaponComponent = weaponComponent;
            _radarModel = radarModel;
            _healthModel = healthModel;
            _selectionModel = selectionModel;
            _selectionService = selectionService;
            _attackDataFactory = attackDataFactory;
        }

        public void Initialize()
        {
            _radarModel.Enemies.ItemAdded += HandleEnemyAdded;
            _selectionService.AddObserver(this);
            _healthModel.OnDestroy += Release;
        }

        public void LateDispose()
        {
            Release();
        }

        public void UpdateState(ISelectionSubject selectionSubject)
        {
            if (!_selectionModel.IsSelected)
            {
                return;
            }

            if (selectionSubject.UpdatedType != PlayerType.Opponent)
            {
                return;
            }

            if (!selectionSubject.EnemySelectionContext.HasSelectable)
            {
                return;
            }

            IEntity target = selectionSubject.EnemySelectionContext.Entity;
            if (target.HealthModel.IsDestroyed || !target.HealthModel.HasUnits)
            {
                return;
            }

            AttackData attackData = _attackDataFactory.ConstructData(target);
            if (attackData != null)
            {
                _weaponComponent.AddTarget(attackData, AttackType.MainTarget);
            }
        }

        private void HandleEnemyAdded(ObservableList<IEntity> sender, ListChangedEventArgs<IEntity> args)
        {
            IEntity enemy = args.item;
            if (enemy.HealthModel.IsDestroyed || !enemy.HealthModel.HasUnits)
            {
                return;
            }

            AttackData attackData = _attackDataFactory.ConstructData(enemy);
            if (attackData != null)
            {
                _weaponComponent.AddTarget(attackData, AttackType.Base);
            }
        }

        private void Release()
        {
            if (_isReleased)
            {
                return;
            }

            _isReleased = true;
            _radarModel.Enemies.ItemAdded -= HandleEnemyAdded;
            _selectionService.RemoveObserver(this);
            _healthModel.OnDestroy -= Release;
            _weaponComponent.Release();
        }
    }
}
