using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.Squadrons.EntityFacades;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.ViewComponents.Squadrons;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using VContainer;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Entities.Squadrons.Data;

namespace EmpireAtWar.Components.Squadrons.Health
{
    /// <summary>
    /// Exposes each fighter as a hardpoint so existing weapons can target and destroy them individually.
    /// </summary>
    [LifecycleOrder(Initialize = LifecycleOrders.HEALTH_INITIALIZE)]
    public sealed class SquadronHealthComponent : MonoComponent<SquadronHealthModel>, ISquadronHealthComponent,
        IHealthModelObserver, IHealthTooltipObserver, ISquadronMembersFacade, IInitializable, ITickable, ILateDisposable
    {
        private ITimer _regenerateShieldsTimer;
        private readonly ITimer _shieldRecoveryTimer = TimerFactory.ConstructTimer();
        private bool _shieldRecovering;

        [SerializeField] private List<FighterView> fighters;
        private readonly List<IHardPointModel> _liveUnits = new List<IHardPointModel>();
        private CombatModifiers _modifiers;
        private HardPointAdapter[] _adapters = Array.Empty<HardPointAdapter>();

        private PlayerId _owner;

        private bool _isReleased;

        public event Action OnValueChanged
        {
            add => Model.OnValueChanged += value;
            remove => Model.OnValueChanged -= value;
        }

        public event Action OnDestroy
        {
            add => Model.OnDestroy += value;
            remove => Model.OnDestroy -= value;
        }

        private SquadronData Data { get; set; }

        public int FighterCount => fighters.Count;
        public IReadOnlyList<IHardPointModel> Members => _adapters;
        public bool Destroyed => Model.IsDestroyed;
        public IHealthModelObserver HealthModelObserver => this;
        public ShipClass ShipClass => Model.ShipClass;
        public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Model.Members;
        public float Hull => Model.Hull;
        public bool IsShieldRecovering => _shieldRecovering && !_shieldRecoveryTimer.IsComplete;
        public float ShieldRegeneration => Model.IsShieldSuppressed || IsShieldRecovering ? 0f
            : Model.ShieldRegenerateValue * _modifiers.ShieldRegenMultiplier * Model.AliveCount;
        public float ShieldRegenerationInterval => Model.ShieldRegenerateDelay;
        public float HullPercentage => Model.HullPercentage;
        public float Shields => Model.Shields;
        public float ShieldPercentage => Model.ShieldPercentage;
        public bool IsDestroyed => Model.IsDestroyed;
        public bool IsLostShieldGenerator => false;
        public bool HasUnits => Model.HasUnits;
        public bool HasLiveHardPoints => Model.HasLiveHardPoints;
        public bool HasShields => Model.HasShields;
        public PlayerId Owner => _owner;

        [Inject]
        private void Construct(SquadronHealthModel model, CombatModifiers modifiers,
            PlayerId owner, SquadronData data)
        {
            Data = data;
            SetModel(model);
            _modifiers = modifiers;
            _owner = owner;
        }

        public void Initialize()
        {
            HardPointModel[] members = new HardPointModel[fighters.Count];
            _adapters = new HardPointAdapter[fighters.Count];
            for (int i = 0; i < fighters.Count; i++)
            {
                members[i] = new HardPointModel(id: fighters[i].Id, hardPointType: fighters[i].HardPointType);
            }

            Model.InitializeMembers(members);
            _modifiers.Changed += Model.RefreshStatModifiers;
            for (int i = 0; i < fighters.Count; i++)
            {
                _adapters[i] = new HardPointAdapter(model: members[i], view: fighters[i],
                    pivot: fighters[i].Transform);
            }

            _regenerateShieldsTimer = TimerFactory.ConstructTimer(Model.ShieldRegenerateDelay);
            _shieldRecovering = false;
            _modifiers.Changed += HandleShieldSuppressionChanged;
            HandleShieldSuppressionChanged();
        }

        private void HandleShieldSuppressionChanged()
        {
            if (Model.IsShieldSuppressed == _modifiers.IsShieldSuppressed) return;
            bool suppressed = _modifiers.IsShieldSuppressed;
            _shieldRecovering = !suppressed;
            if (!suppressed) _shieldRecoveryTimer.ChangeDelay(_modifiers.ShieldRecoveryDelay).StartTimer();
            Model.SetShieldSuppressed(suppressed);
        }

        public void LateDispose() => Release();

        public void Respawn()
        {
            _isReleased = false;
            Initialize();
        }

        public void Tick()
        {
            if (_isReleased || Model.IsDestroyed) return;
            Model.RepairHull(Data.HullRepairPerSecond * Time.deltaTime);
            if (Model.IsShieldSuppressed || IsShieldRecovering || !_regenerateShieldsTimer.IsComplete) return;

            Model.RegenerateShields(Model.ShieldRegenerateValue * _modifiers.ShieldRegenMultiplier);
            _regenerateShieldsTimer.StartTimer();
        }

        public override void Release()
        {
            if (_isReleased)
            {
                return;
            }

            _isReleased = true;
            _modifiers.Changed -= Model.RefreshStatModifiers;
            _modifiers.Changed -= HandleShieldSuppressionChanged;
            foreach (HardPointAdapter adapter in _adapters)
            {
                adapter.Dispose();
            }
        }

        public void ApplyDamage(float damage, DamageType damageType, int shipUnitId) =>
            Model.ApplyDamage(damage, damageType, shipUnitId);

        public void Kill() => Model.Kill();

        public bool Equal(IHealthModelObserver modelObserver) => ReferenceEquals(this, modelObserver);

        /// <summary>Live fighters; once all are dead the wrecks are returned so callers never get an empty set.</summary>
        public IHardPointModel[] GetShipUnits(HardPointType hardPointType)
        {
            CopyLiveUnits(_liveUnits);
            return _liveUnits.Count > 0 ? _liveUnits.ToArray() : _adapters;
        }

        public void CopyShipUnits(List<IHardPointModel> destination)
        {
            CopyLiveUnits(destination);
            if (destination.Count > 0) return;
            // Indexed adds: Mono's AddRange copies an ICollection through a temporary array.
            for (int i = 0; i < _adapters.Length; i++) destination.Add(_adapters[i]);
        }

        private void CopyLiveUnits(List<IHardPointModel> destination)
        {
            destination.Clear();
            foreach (HardPointAdapter adapter in _adapters)
            {
                if (!adapter.IsDestroyed) destination.Add(adapter);
            }
        }
    }
}
