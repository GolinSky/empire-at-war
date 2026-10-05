using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Audio;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Ship.Orders;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Services.Timing;
using EmpireAtWar.Services.UnitWreck;
using EmpireAtWar.Services.UnitExplosion;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;
using IEntity = EmpireAtWar.Entities.BaseEntity.IEntity;
using EmpireAtWar.Entities.BaseEntity.Orders;

namespace EmpireAtWar.Ship
{
    public interface IShipEntity
    {
        PlayerId Owner { get; }
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
        private IHealthComponent _healthComponent;
        private IShipMoveComponent _shipMoveComponent;
        private IRadarComponent _radarComponent;
        private IWeaponComponent _weaponComponent;
        private ISelectionModelObserver _selectionModel;
        private IAudioShipComponent _audioShipComponent;
        private IAudioDialogShipComponent _audioDialogShipComponent;
        private IWeaponFireEvents _weaponFireEvents;
        private IReadOnlyList<ShipAbilitySlot> _audioAbilities;
        private ILayerService _layerService;
        private IUnitExplosionService _unitExplosionService;
        private IUnitWreckService _unitWreckService;

        [SerializeField] private Renderer[] explosionHullRenderers;
        private HardPointModel _enginesUnitModel;
        private ShipOrderRunner _orders;
        private LazyInject<IEntity> _entity;
        private EntityComponentLifecycle _componentLifecycle;
        private GameObjectContext _context;

        private PlayerId _owner;

        private bool _isReleased;

        public event Action<ShipType> OnRelease;

        [Inject] private IShipService ShipService { get; }
        [Inject] private IShipData Data { get; }
        [Inject] private ShipType ShipType { get; }

        public string Id => GetType().Name;
        public PlayerId Owner => _owner;
        public Vector3 WorldPosition => _shipMoveComponent.CurrentPosition;
        public float NavigationRadius => _shipMoveComponent.NavigationRadius;
        public float NavigationSpeed => _shipMoveComponent.NavigationSpeed;
        public long EntityId => _entity.Value.Id;
        public UnitOrderType CurrentOrder => _orders.CurrentOrder;

        [Inject]
        private void Construct(
            IHealthComponent healthComponent,
            IShipMoveComponent shipMoveComponent,
            IRadarComponent radarComponent,
            IWeaponComponent weaponComponent,
            ISelectionModelObserver selectionModel,
            IAudioShipComponent audioShipComponent,
            IWeaponFireEvents weaponFireEvents,
            [InjectOptional] IAudioDialogShipComponent audioDialogShipComponent,
            ILayerService layerService,
            IUnitWreckService unitWreckService,
            IUnitExplosionService unitExplosionService,
            ShipOrderRunner orders,
            LazyInject<IEntity> entity,
            List<IMonoComponent> monoComponents,
            GameObjectContext context,
            PlayerId owner)
        {
            _healthComponent = healthComponent;
            _shipMoveComponent = shipMoveComponent;
            _radarComponent = radarComponent;
            _weaponComponent = weaponComponent;
            _selectionModel = selectionModel;
            _orders = orders;
            _entity = entity;
            _owner = owner;
            _audioShipComponent = audioShipComponent;
            _weaponFireEvents = weaponFireEvents;
            _audioDialogShipComponent = audioDialogShipComponent;
            _componentLifecycle = new EntityComponentLifecycle(monoComponents);
            _layerService = layerService;
            _unitWreckService = unitWreckService;
            _unitExplosionService = unitExplosionService;
            _context = context;
        }

        public void Initialize()
        {
            _audioAbilities = _entity.Value.GetFacade<IShipAbilityFacade>().Slots;
            _audioShipComponent.InitializeAudio(_entity.Value, _audioAbilities);
            _weaponFireEvents.ShotEmitted += _audioShipComponent.PlayWeaponShot;
            foreach (ShipAbilitySlot ability in _audioAbilities)
                ability.Changed += _audioShipComponent.HandleAbilityChanged;
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
            _shipMoveComponent.HyperSpaceCompleted += _audioShipComponent.PlayHyperSpace;
            if (_audioDialogShipComponent != null)
            {
                _shipMoveComponent.DestinationChanged += _audioDialogShipComponent.HandleMove;
                _shipMoveComponent.LookingAt += _audioDialogShipComponent.HandleAttack;
                _shipMoveComponent.Stopped += _audioDialogShipComponent.HandleStopped;
                _selectionModel.OnSelected += _audioDialogShipComponent.HandleSelection;
            }

            _orders.Start();
            ShipService.Add(this);
            SynchronizeComponents();
        }

        public void LateDispose()
        {
            Release(false);
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
            }

            ShipService.Remove(this);

            if (playDeathEffects && gameObject.activeInHierarchy)
            {
                OnRelease?.Invoke(ShipType);
                _unitExplosionService.Spawn(explosionHullRenderers);
                // The explosion hides the swap: the wreck appears as the ship entity is destroyed.
                if (Data.Wreck != null)
                {
                    _unitWreckService.Spawn(Data.Wreck, transform, _owner, Data.DestroyDelay);
                }

                Destroy(_context.gameObject, Data.DestroyDelay);
            }
        }

        private void Unsubscribe()
        {
            _weaponFireEvents.ShotEmitted -= _audioShipComponent.PlayWeaponShot;
            foreach (ShipAbilitySlot ability in _audioAbilities)
                ability.Changed -= _audioShipComponent.HandleAbilityChanged;
            if (_enginesUnitModel != null)
            {
                _enginesUnitModel.OnHardPointHealthChanged -= HandleEnginesData;
            }

            _radarComponent.Enemies.ItemAdded -= HandleEnemyAdded;
            _radarComponent.ContactsUpdated -= _shipMoveComponent.HandleRadarContacts;
            _selectionModel.OnSelected -= _shipMoveComponent.HandleSelection;
            _shipMoveComponent.HyperSpaceCompleted -= _audioShipComponent.PlayHyperSpace;
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
                    new AttackData(healthModel, healthFacade, HardPointType.Any, enemy),
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
            _audioShipComponent.UpdateAudio();
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
