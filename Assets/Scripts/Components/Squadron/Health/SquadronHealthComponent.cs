using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Factions;
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
        IHealthModelObserver, IInitializable, ITickable, ILateDisposable
    {
        [SerializeField] private List<FighterView> fighters;

        private readonly List<IHardPointModel> _liveUnits = new List<IHardPointModel>();
        private ITimer _regenerateShieldsTimer;
        private CombatModifiers _modifiers;
        private PlayerType _playerType;
        private HardPointAdapter[] _adapters = Array.Empty<HardPointAdapter>();
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
        public float HullPercentage => Model.HullPercentage;
        public float Shields => Model.Shields;
        public float ShieldPercentage => Model.ShieldPercentage;
        public bool IsDestroyed => Model.IsDestroyed;
        public bool IsLostShieldGenerator => false;
        public bool HasUnits => Model.HasUnits;
        public bool HasLiveHardPoints => Model.HasLiveHardPoints;
        public bool HasShields => Model.HasShields;
        public PlayerType PlayerType => _playerType;

        [Inject]
        private void Construct(SquadronHealthModel model, CombatModifiers modifiers,
            PlayerType playerType)
        {
            SetModel(model);
            _modifiers = modifiers;
            _playerType = playerType;
        }

        public void Initialize()
        {
            HardPointModel[] members = new HardPointModel[fighters.Count];
            _adapters = new HardPointAdapter[fighters.Count];
            for (int i = 0; i < fighters.Count; i++)
            {
                members[i] = new HardPointModel(fighters[i].Id, fighters[i].HardPointType);
            }

            Model.InitializeMembers(members);
            for (int i = 0; i < fighters.Count; i++)
            {
                _adapters[i] = new HardPointAdapter(members[i], fighters[i]);
            }

            _regenerateShieldsTimer = TimerFactory.ConstructTimer(Model.ShieldRegenerateDelay);
        }

        public void Tick()
        {
            if (_isReleased || Model.IsDestroyed || !_regenerateShieldsTimer.IsComplete)
            {
                return;
            }

            Model.RegenerateShields(Model.ShieldRegenerateValue * _modifiers.ShieldRegenMultiplier);
            _regenerateShieldsTimer.StartTimer();
        }

        public void LateDispose() => Release();

        public override void Release()
        {
            if (_isReleased)
            {
                return;
            }

            _isReleased = true;
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
