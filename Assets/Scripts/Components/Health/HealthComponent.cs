using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
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
        void ApplyDamage(float damage, WeaponType weaponType, int shipUnitId);
        bool Equal(IHealthModelObserver modelObserver);
        void SetMovementState(bool isMoving);
        bool Destroyed { get; }
        IHealthModelObserver HealthModelObserver { get; }
    }

    public class HealthComponent : MonoComponent<HealthModel>, IInitializable, ILateDisposable,
        IHealthComponent, IHealthModelObserver, ITickable
    {
        [field: SerializeField] public List<HardPointView> ShipUnits { get; set; }
        [SerializeField] private ShieldView shieldView;

        private ITimer _refreshShieldsTimer;
        private bool _isMoving;
        private float _originShieldValue;
        private Coroutine _shieldsAnimatedCoroutine;
        private bool _isReleased;
        private IEntityLifecycle _entityLifecycle;
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
        public HardPointModel[] HardPointModels => Model.HardPointModels;
        public float Armor => Model.Armor;
        public float ArmorPercentage => Model.ArmorPercentage;
        public float Shields => Model.Shields;
        public float ShieldPercentage => Model.ShieldPercentage;
        public bool IsDestroyed => Model.IsDestroyed;
        public bool IsLostShieldGenerator => Model.IsLostShieldGenerator;
        public bool HasUnits => Model.HasUnits;
        public bool HasShields => Model.HasShields;
        public PlayerType PlayerType => _playerType;
        public Transform Transform => _viewTransform;

        [Inject]
        private void Construct(
            HealthModel model,
            IEntityLifecycle entityLifecycle,
            PlayerType playerType,
            [Inject(Id = EntityBindType.ViewTransform)] Transform viewTransform)
        {
            SetModel(model);
            _entityLifecycle = entityLifecycle;
            _playerType = playerType;
            _viewTransform = viewTransform;
        }

        public void Initialize()
        {
            InitializeHardPoints();
            _originShieldValue = Model.Shields;
            _refreshShieldsTimer = TimerFactory.ConstructTimer(Model.ShieldRegenerateDelay);

            Model.OnValueChanged += UpdateData;
            Model.OnDestroy += HandleDestroy;

            if (shieldView != null)
            {
                _shieldsAnimatedCoroutine = StartCoroutine(AnimateShields());
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
            Model.OnValueChanged -= UpdateData;
            Model.OnDestroy -= HandleDestroy;

            if (_shieldsAnimatedCoroutine != null)
            {
                StopCoroutine(_shieldsAnimatedCoroutine);
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

        public void ApplyDamage(float damage, WeaponType weaponType, int shipUnitId)
        {
            Model.ApplyDamage(damage, weaponType, _isMoving, shipUnitId);
        }

        public void SetMovementState(bool isMoving)
        {
            _isMoving = isMoving;
        }

        public bool Equal(IHealthModelObserver modelObserver)
        {
            return this == modelObserver;
        }

        public void Tick()
        {
            if (!Model.IsLostShieldGenerator && Model.Shields < _originShieldValue &&
                _refreshShieldsTimer.IsComplete)
            {
                _refreshShieldsTimer.StartTimer();
            }
        }

        public IHardPointModel[] GetShipUnits(HardPointType hardPointType)
        {
            IHardPointModel[] currentHardPoints = _hardPointAdapters
                .Where(hardPoint => !hardPoint.IsDestroyed)
                .Cast<IHardPointModel>()
                .ToArray();
            if (currentHardPoints.Length == 0)
            {
                return null;
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
                IHardPointView hardPointView = ShipUnits[index];
                HardPointModel hardPointModel = new HardPointModel(
                    hardPointView.Id,
                    hardPointView.HardPointType);
                hardPointModels[index] = hardPointModel;
                _hardPointAdapters[index] = new HardPointAdapter(hardPointModel, hardPointView);
            }

            Model.InitializeHardPoints(hardPointModels);
        }

        private void HandleDestroy()
        {
            _entityLifecycle.Release();
            Release();
        }

        private IEnumerator AnimateShields()
        {
            while (!Model.IsDestroyed && !Model.IsLostShieldGenerator)
            {
                if (shieldView.IsVisibleToCamera && Model.Shields > 0f)
                {
                    shieldView.AnimateTextureOffset();
                }

                yield return new WaitForEndOfFrame();
            }
        }

        private void UpdateData()
        {
            if (shieldView != null)
            {
                shieldView.SetActive(Model.Shields > 0f);
            }
        }
    }
}
