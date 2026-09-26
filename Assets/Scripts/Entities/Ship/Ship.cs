using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Audio;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Ship.Orders;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Services.Timing;
using EmpireAtWar.Services.UnitDeathAnimation;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;
using IEntity = EmpireAtWar.Entities.BaseEntity.IEntity;
using EmpireAtWar.Entities.BaseEntity.Orders;

namespace EmpireAtWar.Ship
{
    public interface IShipEntity
    {
        IShipModelObserver ModelObserver { get; }
        PlayerType PlayerType { get; }
        Vector3 WorldPosition { get; }
        float NavigationRadius { get; }
        float NavigationSpeed { get; }
        long EntityId { get; }
        UnitOrderType CurrentOrder { get; }
    }

    /// <summary>
    /// Ship lifecycle and component wiring. Orders and states live in <see cref="ShipOrderRunner"/>.
    /// </summary>
    public class Ship : MonoBehaviour, IController, IShipEntity, IInitializable, ILateDisposable, ITickable
    {
        private HardPointModel _enginesUnitModel;
        private IHealthComponent _healthComponent;
        private IShipMoveComponent _shipMoveComponent;
        private IRadarComponent _radarComponent;
        private IWeaponComponent _weaponComponent;
        private ISelectionModelObserver _selectionModel;
        private ShipOrderRunner _orders;
        private LazyInject<IEntity> _entity;
        private IAudioShipComponent _audioShipComponent;
        private IAudioDialogShipComponent _audioDialogShipComponent;
        private EntityComponentLifecycle _componentLifecycle;
        private PlayerType _playerType;
        private bool _isReleased;
        private ILayerService _layerService;
        private IUnitDeathAnimationData _deathAnimationData;
        private IUnitDeathAnimationService _deathAnimationService;

        [Inject] private IShipService ShipService { get; }
        [Inject] private IShipData Data { get; }
        [Inject] private ShipType ShipType { get; }
        [Inject] private ShipData RootModel { get; }

        public event Action<ShipType> OnRelease;

        public string Id => GetType().Name;
        public PlayerType PlayerType => _playerType;
        public Vector3 WorldPosition => _shipMoveComponent.CurrentPosition;
        public float NavigationRadius => _shipMoveComponent.NavigationRadius;
        public float NavigationSpeed => _shipMoveComponent.NavigationSpeed;
        public long EntityId => _entity.Value.Id;
        public UnitOrderType CurrentOrder => _orders.CurrentOrder;
        IShipModelObserver IShipEntity.ModelObserver => RootModel;

        [Inject]
        private void Construct(
            IHealthComponent healthComponent,
            IShipMoveComponent shipMoveComponent,
            IRadarComponent radarComponent,
            IWeaponComponent weaponComponent,
            ISelectionModelObserver selectionModel,
            ShipOrderRunner orders,
            LazyInject<IEntity> entity,
            PlayerType playerType,
            IAudioShipComponent audioShipComponent,
            [InjectOptional] IAudioDialogShipComponent audioDialogShipComponent,
            List<IMonoComponent> monoComponents,
            ILayerService layerService,
            IUnitDeathAnimationData deathAnimationData,
            IUnitDeathAnimationService deathAnimationService)
        {
            _healthComponent = healthComponent;
            _shipMoveComponent = shipMoveComponent;
            _radarComponent = radarComponent;
            _weaponComponent = weaponComponent;
            _selectionModel = selectionModel;
            _orders = orders;
            _entity = entity;
            _playerType = playerType;
            _audioShipComponent = audioShipComponent;
            _audioDialogShipComponent = audioDialogShipComponent;
            _componentLifecycle = new EntityComponentLifecycle(monoComponents);
            _layerService = layerService;
            _deathAnimationData = deathAnimationData;
            _deathAnimationService = deathAnimationService;
        }

        public IModel GetModel()
        {
            return RootModel;
        }

        public void Initialize()
        {
            _healthComponent.HealthModelObserver.OnDestroy += HandleDestroyed;
            foreach (HardPointModel hardPointModel in _healthComponent.HealthModelObserver.HardPointModels)
            {
                if (hardPointModel.HardPointType == HardPointType.Engines)
                {
                    _enginesUnitModel = hardPointModel;
                    _enginesUnitModel.OnHardPointHealthChanged += HandleEnginesData;
                    break;
                }
            }

            _radarComponent.Enemies.ItemAdded += HandleEnemyAdded;
            _radarComponent.ContactsUpdated += _shipMoveComponent.HandleRadarContacts;
            _selectionModel.OnSelected += _shipMoveComponent.HandleSelection;
            if (_audioDialogShipComponent != null)
            {
                _shipMoveComponent.DestinationChanged += _audioDialogShipComponent.HandleMove;
                _shipMoveComponent.LookingAt += _audioDialogShipComponent.HandleAttack;
                _shipMoveComponent.Stopped += _audioDialogShipComponent.HandleStopped;
                _selectionModel.OnSelected += _audioDialogShipComponent.HandleSelection;
            }

            _orders.Start();
            ShipService.Add(this);
            _audioShipComponent.PlayHyperSpace(_shipMoveComponent.HyperSpaceDuration);
            SynchronizeComponents();
        }

        public void Tick()
        {
            if (_isReleased)
            {
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.ShipTick.Auto())
            {
#endif
            _orders.Tick(Time.deltaTime);
            SynchronizeComponents();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
        }

        public void LateDispose()
        {
            Release(false);
        }

        private void HandleDestroyed() => Release(true);

        private void Release(bool playDeathEffects)
        {
            _healthComponent.HealthModelObserver.OnDestroy -= HandleDestroyed;
            _isReleased = true;
            _orders.Release();
            if (!_componentLifecycle.Release())
            {
                return;
            }

            Unsubscribe();
            if (playDeathEffects)
            {
                _layerService.Apply(gameObject, LayerKey.Dead, true);
                _deathAnimationService.Play(transform, _deathAnimationData);
            }

            ShipService.Remove(this);

            if (playDeathEffects && gameObject.activeInHierarchy)
            {
                OnRelease?.Invoke(ShipType);
                Instantiate(Data.DeathExplosionVfx, transform.position, Quaternion.identity);
            }
        }

        private void Unsubscribe()
        {
            if (_enginesUnitModel != null)
            {
                _enginesUnitModel.OnHardPointHealthChanged -= HandleEnginesData;
            }

            _radarComponent.Enemies.ItemAdded -= HandleEnemyAdded;
            _radarComponent.ContactsUpdated -= _shipMoveComponent.HandleRadarContacts;
            _selectionModel.OnSelected -= _shipMoveComponent.HandleSelection;
            if (_audioDialogShipComponent != null)
            {
                _shipMoveComponent.DestinationChanged -= _audioDialogShipComponent.HandleMove;
                _shipMoveComponent.LookingAt -= _audioDialogShipComponent.HandleAttack;
                _shipMoveComponent.Stopped -= _audioDialogShipComponent.HandleStopped;
                _selectionModel.OnSelected -= _audioDialogShipComponent.HandleSelection;
            }
        }

        private void HandleEnemyAdded(ObservableList<IEntity> sender, ListChangedEventArgs<IEntity> args)
        {
            IEntity enemy = args.item;
            IHealthModelObserver healthModel = enemy.HealthModel;
            if (healthModel.HasUnits && enemy.TryGetFacade(out IHealthFacade healthFacade))
            {
                _weaponComponent.AddTarget(
                    new AttackData(healthModel, healthFacade, HardPointType.Any),
                    AttackType.Base);
            }

            _audioShipComponent.HandleEnemyDetected();
            if (_audioDialogShipComponent != null)
            {
                _audioDialogShipComponent.HandleEnemyDetected();
            }
        }

        private void SynchronizeComponents()
        {
            _radarComponent.SetPosition(_shipMoveComponent.CurrentPosition);
        }

        private void HandleEnginesData()
        {
            if (_enginesUnitModel.IsDestroyed)
            {
                _shipMoveComponent.ApplyMoveCoefficient(Data.MinMoveCoefficient);
            }
        }
    }
}
