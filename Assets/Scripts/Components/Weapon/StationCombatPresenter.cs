using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using UnityEngine.Rendering;
using Zenject;

namespace EmpireAtWar.Components.Weapon
{
    public sealed class StationCombatPresenter : IInitializable, ILateDisposable
    {
        private readonly IWeaponComponent _weaponComponent;
        private readonly IRadarModelObserver _radarModel;
        private readonly IHealthModelObserver _healthModel;
        private readonly IAttackDataFactory _attackDataFactory;
        private bool _isReleased;

        public StationCombatPresenter(
            IWeaponComponent weaponComponent,
            IRadarModelObserver radarModel,
            IHealthModelObserver healthModel,
            IAttackDataFactory attackDataFactory)
        {
            _weaponComponent = weaponComponent;
            _radarModel = radarModel;
            _healthModel = healthModel;
            _attackDataFactory = attackDataFactory;
        }

        public void Initialize()
        {
            _radarModel.Enemies.ItemAdded += HandleEnemyAdded;
            _healthModel.OnDestroy += Release;
        }

        public void LateDispose()
        {
            Release();
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
            _healthModel.OnDestroy -= Release;
            _weaponComponent.Release();
        }
    }
}
