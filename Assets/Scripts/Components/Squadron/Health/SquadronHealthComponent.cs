using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.ViewComponents.Squadrons;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Components.Squadrons.Health
{
    /// <summary>
    /// Exposes each fighter as a hardpoint so existing weapons can target and destroy them individually.
    /// </summary>
    public sealed class SquadronHealthComponent : MonoComponent<SquadronHealthModel>, IHealthComponent,
        IHealthModelObserver, IHealthTooltipObserver, IInitializable, ITickable, ILateDisposable
    {
        private ITimer _regenerateShieldsTimer;

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

        public bool Destroyed => Model.IsDestroyed;
        public IHealthModelObserver HealthModelObserver => this;
        public ShipClass ShipClass => Model.ShipClass;
        public HardPointModel[] HardPointModels => Model.Members;
        public float Hull => Model.Hull;
        public float MaxHull => Model.MaxHull;
        public float MaxShields => Model.MaxShields;
        public float ShieldRegeneration => Model.ShieldRegenerateValue * _modifiers.ShieldRegenMultiplier * Model.AliveCount;
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
            PlayerId owner)
        {
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
                _adapters[i] = new HardPointAdapter(model: members[i], view: fighters[i]);
            }

            _regenerateShieldsTimer = TimerFactory.ConstructTimer(Model.ShieldRegenerateDelay);
        }

        public void LateDispose() => Release();

        public void Tick()
        {
            if (_isReleased || Model.IsDestroyed || !_regenerateShieldsTimer.IsComplete)
            {
                return;
            }

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
            foreach (HardPointAdapter adapter in _adapters)
            {
                adapter.Dispose();
            }
        }

        public void ApplyDamage(float damage, DamageType damageType, int shipUnitId) =>
            Model.ApplyDamage(damage, damageType, shipUnitId);

        public bool Equal(IHealthModelObserver modelObserver) => ReferenceEquals(this, modelObserver);

        /// <summary>Live fighters; once all are dead the wrecks are returned so callers never get an empty set.</summary>
        public IHardPointModel[] GetShipUnits(HardPointType hardPointType)
        {
            _liveUnits.Clear();
            foreach (HardPointAdapter adapter in _adapters)
            {
                if (!adapter.IsDestroyed) _liveUnits.Add(adapter);
            }

            return _liveUnits.Count > 0 ? _liveUnits.ToArray() : _adapters;
        }
    }
}
