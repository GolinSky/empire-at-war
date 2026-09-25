using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.ViewComponents;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Components.Ship.Health
{
    public interface IHealthComponent : IComponent
    {
        void ApplyDamage(float damage, DamageType damageType, int shipUnitId);
        bool Equal(IHealthModelObserver modelObserver);
        bool Destroyed { get; }
        IHealthModelObserver HealthModelObserver { get; }
    }

    public class HealthComponent : MonoComponent<HealthModel>, IInitializable, ILateDisposable,
        IHealthComponent, IHealthModelObserver, IShieldTarget, ITickable
    {
        [field: SerializeField] public List<HardPoint> ShipUnits { get; set; }
        [SerializeField] private Shield shieldView;

        private ITimer _refreshShieldsTimer;
        private ShieldComponent _shield;
        private bool _isReleased;
        private IEntityLifecycle _entityLifecycle;
        private CombatModifiers _modifiers;
        private PlayerType _playerType;
        private Transform _viewTransform;
        private HardPointAdapter[] _hardPointAdapters;

        public event Action OnValueChanged
        {
            add => Model.OnValueChanged += value;
            remove => Model.OnValueChanged -= value;
        }

        event Action IHealthModelObserver.OnDestroy
        {
            add => Model.OnDestroy += value;
            remove => Model.OnDestroy -= value;
        }

        public bool Destroyed => Model.IsDestroyed;
        public IHealthModelObserver HealthModelObserver => this;
        public ShipClass ShipClass => Model.ShipClass;
        public HardPointModel[] HardPointModels => Model.HardPointModels;
        public float Hull => Model.Hull;
        public float HullPercentage => Model.HullPercentage;
        public float Shields => Model.Shields;
        public float ShieldPercentage => Model.ShieldPercentage;
        public bool IsDestroyed => Model.IsDestroyed;
        public bool IsLostShieldGenerator => Model.IsLostShieldGenerator;
        public bool HasUnits => Model.HasUnits;
        public bool HasLiveHardPoints => Model.HasLiveHardPoints;
        public bool HasShields => Model.HasShields;
        public PlayerType PlayerType => _playerType;
        public Transform Transform => _viewTransform;

        [Inject]
        private void Construct(
            HealthModel model,
            CombatModifiers modifiers,
            IEntityLifecycle entityLifecycle,
            PlayerType playerType,
            [Inject(Id = EntityBindType.ViewTransform)] Transform viewTransform)
        {
            SetModel(model);
            _modifiers = modifiers;
            _entityLifecycle = entityLifecycle;
            _playerType = playerType;
            _viewTransform = viewTransform;
        }

        public void Initialize()
        {
            InitializeHardPoints();
            _refreshShieldsTimer = TimerFactory.ConstructTimer(Model.ShieldRegenerateDelay);

            Model.OnDestroy += HandleDestroy;

            if (shieldView != null)
            {
                _shield = new ShieldComponent(this, shieldView);
                _shield.Initialize();
            }
        }

        public void LateDispose()
        {
            Release();
        }

        public override void Release()
        {
            if (_isReleased)
            {
                return;
            }

            _isReleased = true;
            Model.OnDestroy -= HandleDestroy;

            if (_shield != null)
            {
                _shield.Dispose();
            }

            if (_hardPointAdapters == null)
            {
                return;
            }

            foreach (HardPointAdapter hardPointAdapter in _hardPointAdapters)
            {
                hardPointAdapter.Dispose();
            }
        }

        public void ApplyDamage(float damage, DamageType damageType, int shipUnitId)
        {
            Model.ApplyDamage(damage, damageType, shipUnitId);
        }

        public bool Equal(IHealthModelObserver modelObserver)
        {
            return this == modelObserver;
        }

        public void Tick()
        {
            if (!Model.IsLostShieldGenerator && Model.Shields < Model.MaxShields &&
                _refreshShieldsTimer.IsComplete)
            {
                Model.RegenerateShields(Model.ShieldRegenerateValue * _modifiers.ShieldRegenMultiplier);
                _refreshShieldsTimer.StartTimer();
            }
        }

        /// <summary>
        /// Live hardpoints of the requested type (or all live ones when none match).
        /// Once every hardpoint is destroyed the wrecks stay targetable so the hull can still be finished off.
        /// </summary>
        public IHardPointModel[] GetShipUnits(HardPointType hardPointType)
        {
            IHardPointModel[] currentHardPoints = _hardPointAdapters
                .Where(hardPoint => !hardPoint.IsDestroyed)
                .Cast<IHardPointModel>()
                .ToArray();
            if (currentHardPoints.Length == 0)
            {
                return _hardPointAdapters.Cast<IHardPointModel>().ToArray();
            }

            if (hardPointType == HardPointType.Any ||
                currentHardPoints.All(hardPoint => hardPoint.HardPointType != hardPointType))
            {
                return currentHardPoints;
            }

            return currentHardPoints
                .Where(hardPoint => hardPoint.HardPointType == hardPointType)
                .ToArray();
        }

        private void InitializeHardPoints()
        {
            HardPointModel[] hardPointModels = new HardPointModel[ShipUnits.Count];
            _hardPointAdapters = new HardPointAdapter[ShipUnits.Count];
            for (int index = 0; index < ShipUnits.Count; index++)
            {
                IHardPoint hardPoint = ShipUnits[index];
                // Damage is routed by list index, so the model id must be the index, not the serialized view id.
                HardPointModel hardPointModel = new HardPointModel(
                    index,
                    hardPoint.HardPointType);
                hardPointModels[index] = hardPointModel;
                _hardPointAdapters[index] = new HardPointAdapter(hardPointModel, hardPoint);
            }

            Model.InitializeHardPoints(hardPointModels);
        }

        private void HandleDestroy()
        {
            _entityLifecycle.Release();
            Release();
        }

        public Vector3 GetImpactPosition(Vector3 origin, Vector3 target, DamageType damageType)
        {
            return !_isReleased && _shield != null && Model.AbsorbsDamage(damageType)
                ? _shield.GetImpactPosition(origin, target)
                : target;
        }

        public bool ShowShieldImpact(Vector3 position)
        {
            if (_shield == null) return false;
            if (!_isReleased) _shield.ShowImpact(position);
            return true;
        }
    }
}
