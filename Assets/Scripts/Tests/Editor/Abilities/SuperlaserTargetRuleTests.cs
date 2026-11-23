using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>Xyston superlaser: frigates and larger plus stations; auto target prefers capital ships.</summary>
    [Category("Abilities")]
    public sealed class SuperlaserTargetRuleTests
    {
        private readonly SuperlaserSettings _settings = new SuperlaserSettings();

        [TestCase(ShipClass.Fighter)]
        [TestCase(ShipClass.Bomber)]
        [TestCase(ShipClass.Interceptor)]
        [TestCase(ShipClass.Corvette)]
        public void SmallCraft_AreNotValidTargets(ShipClass shipClass) =>
            Assert.That(_settings.CanTarget(null, Ship(shipClass)), Is.False);

        [TestCase(ShipClass.Frigate)]
        [TestCase(ShipClass.Cruiser)]
        [TestCase(ShipClass.HeavyCruiser)]
        [TestCase(ShipClass.Capital)]
        [TestCase(ShipClass.BattleCruiser)]
        [TestCase(ShipClass.Dreadnought)]
        public void FrigatesAndLarger_AreValidTargets(ShipClass shipClass) =>
            Assert.That(_settings.CanTarget(null, Ship(shipClass)), Is.True);

        [Test]
        public void Station_IsAValidTarget() =>
            Assert.That(_settings.CanTarget(null, Station()), Is.True);

        [Test]
        public void AutoTarget_PrefersCapitalThenBattleCruiserThenDreadnoughtThenStation()
        {
            int capital = _settings.GetTargetPriority(Ship(ShipClass.Capital));
            int battleCruiser = _settings.GetTargetPriority(Ship(ShipClass.BattleCruiser));
            int dreadnought = _settings.GetTargetPriority(Ship(ShipClass.Dreadnought));
            int station = _settings.GetTargetPriority(Station());
            int frigate = _settings.GetTargetPriority(Ship(ShipClass.Frigate));

            Assert.That(capital, Is.GreaterThan(battleCruiser));
            Assert.That(battleCruiser, Is.GreaterThan(dreadnought));
            Assert.That(dreadnought, Is.GreaterThan(station));
            Assert.That(station, Is.GreaterThan(frigate));
        }

        private static IEntity Ship(ShipClass shipClass) => new FakeEntity(new FakeHealth(shipClass));

        private static IEntity Station() => new FakeEntity(new FakeHealth(ShipClass.Structure), new PlayerBaseFacade());

        private sealed class FakeEntity : IEntity
        {
            private readonly IEntityFacade[] _facades;

            public long Id => 1;
            public IHealthModelObserver HealthModel { get; }
            public PlayerId Owner => TestPlayers.Human;

            public FakeEntity(FakeHealth health, params IEntityFacade[] facades)
            {
                HealthModel = health;
                _facades = facades;
            }

            public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade
            {
                TryGetFacade(out TFacade facade);
                return facade;
            }

            public bool TryGetFacade<TFacade>(out TFacade facade) where TFacade : IEntityFacade
            {
                foreach (IEntityFacade candidate in _facades)
                    if (candidate is TFacade matching)
                    {
                        facade = matching;
                        return true;
                    }
                facade = default;
                return false;
            }
        }

        private sealed class FakeHealth : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }

            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<IReadOnlyHardPointModel>();
            public float Hull => 1f;
            public ShipClass ShipClass { get; }
            public bool HasLiveHardPoints => true;
            public float HullPercentage => 1f;
            public float Shields => 1f;
            public float ShieldPercentage => 1f;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public bool HasShields => true;
            public PlayerId Owner => TestPlayers.Human;

            public FakeHealth(ShipClass shipClass) => ShipClass = shipClass;

            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) => Array.Empty<IHardPointModel>();
        }
    }
}
