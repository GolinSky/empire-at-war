using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.TeamColor;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ships")]
    public sealed class UnitHullDamageSyncTests
    {
        private HealthStub _health;
        private RenderState _renderState;
        private UnitHullDamageSync _sync;

        [SetUp]
        public void SetUp()
        {
            _health = new HealthStub { HullPercentage = 0.75f };
            _renderState = new RenderState();
            _sync = new UnitHullDamageSync(new HealthComponentStub(_health), _renderState);
        }

        [Test]
        public void Initialize_ShowsCurrentHull()
        {
            _sync.Initialize();

            Assert.That(_renderState.HullPercentage, Is.EqualTo(0.75f));
        }

        [Test]
        public void HealthChange_UpdatesHullDamage()
        {
            _sync.Initialize();

            _health.HullPercentage = 0.25f;
            _health.RaiseValueChanged();

            Assert.That(_renderState.HullPercentage, Is.EqualTo(0.25f));
        }

        [Test]
        public void LateDispose_StopsFollowingHealth()
        {
            _sync.Initialize();
            _sync.LateDispose();

            _health.HullPercentage = 0.25f;
            _health.RaiseValueChanged();

            Assert.That(_renderState.HullPercentage, Is.EqualTo(0.75f));
        }

        private sealed class RenderState : IUnitRenderStateComponent
        {
            public float HullPercentage { get; private set; } = -1f;

            public uint WreckUserValue => 0u;

            public void SetHighlight(UnitHighlight flags) { }

            public void SetHullDamage(float hullPercentage) => HullPercentage = hullPercentage;

            public void SetIonStunned(bool stunned) { }
        }

        private sealed class HealthComponentStub : IHealthComponent
        {
            public HealthComponentStub(IHealthModelObserver health)
            {
                HealthModelObserver = health;
            }

            public string Id => nameof(HealthComponentStub);
            public bool Destroyed => false;
            public IHealthModelObserver HealthModelObserver { get; }

            public void ApplyDamage(float damage, DamageType damageType, int shipUnitId) { }

            public bool Equal(IHealthModelObserver modelObserver) => false;
        }

        private sealed class HealthStub : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }

            public event Action OnValueChanged;

            public ShipClass ShipClass => ShipClass.Structure;
            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1f;
            public float HullPercentage { get; set; }
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public bool HasLiveHardPoints => true;
            public bool HasShields => false;
            public PlayerId Owner => TestPlayers.Human;

            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) => Array.Empty<IHardPointModel>();

            public void RaiseValueChanged() => OnValueChanged?.Invoke();
        }
    }
}
