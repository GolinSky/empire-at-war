using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>
    /// Xyston superlaser: frigates and larger plus stations and structures; auto target ranks like Ion Pulse:
    /// station or structure, then Dreadnought > BattleCruiser > Capital.
    /// </summary>
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
        public void Structure_IsAValidTarget() =>
            Assert.That(_settings.CanTarget(null, Ship(ShipClass.Structure)), Is.True);

        [Test]
        public void AutoTarget_PrefersStationOrStructureThenDreadnoughtThenBattleCruiserThenCapital()
        {
            int capital = _settings.GetTargetPriority(Ship(ShipClass.Capital));
            int battleCruiser = _settings.GetTargetPriority(Ship(ShipClass.BattleCruiser));
            int dreadnought = _settings.GetTargetPriority(Ship(ShipClass.Dreadnought));
            int station = _settings.GetTargetPriority(Station());
            int structure = _settings.GetTargetPriority(Ship(ShipClass.Structure));
            int frigate = _settings.GetTargetPriority(Ship(ShipClass.Frigate));

            Assert.That(station, Is.GreaterThan(dreadnought));
            Assert.That(structure, Is.EqualTo(station));
            Assert.That(dreadnought, Is.GreaterThan(battleCruiser));
            Assert.That(battleCruiser, Is.GreaterThan(capital));
            Assert.That(capital, Is.GreaterThan(frigate));
        }

        [Test]
        public void AutoTarget_RanksShipsAndStationsInTheSameOrderAsIonPulse()
        {
            var ionPulse = new IonPulseSettings();
            IEntity[] ordered =
            {
                Station(), Ship(ShipClass.Dreadnought), Ship(ShipClass.BattleCruiser), Ship(ShipClass.Capital),
                Ship(ShipClass.Frigate)
            };
            for (int i = 1; i < ordered.Length; i++)
            {
                Assert.That(_settings.GetTargetPriority(ordered[i - 1]),
                    Is.GreaterThan(_settings.GetTargetPriority(ordered[i])));
                Assert.That(ionPulse.GetTargetPriority(ordered[i - 1]),
                    Is.GreaterThan(ionPulse.GetTargetPriority(ordered[i])));
            }
        }

        [Test]
        public void DamageMultiplier_StationOverridesStructureAndEachClassUsesItsOwnScale()
        {
            Assert.That(_settings.GetDamageMultiplier(Station()), Is.EqualTo(_settings.VsStation));
            Assert.That(_settings.GetDamageMultiplier(Ship(ShipClass.Structure)), Is.EqualTo(_settings.VsStructure));
            Assert.That(_settings.GetDamageMultiplier(Ship(ShipClass.Dreadnought)), Is.EqualTo(_settings.VsDreadnought));
            Assert.That(_settings.GetDamageMultiplier(Ship(ShipClass.BattleCruiser)), Is.EqualTo(_settings.VsBattleCruiser));
            Assert.That(_settings.GetDamageMultiplier(Ship(ShipClass.Capital)), Is.EqualTo(_settings.VsCapital));
            Assert.That(_settings.GetDamageMultiplier(Ship(ShipClass.HeavyCruiser)), Is.EqualTo(_settings.VsHeavyCruiser));
            Assert.That(_settings.GetDamageMultiplier(Ship(ShipClass.Cruiser)), Is.EqualTo(_settings.VsCruiser));
            Assert.That(_settings.GetDamageMultiplier(Ship(ShipClass.Frigate)), Is.EqualTo(_settings.VsOther));
            Assert.That(_settings.VsStation, Is.GreaterThan(_settings.VsStructure));
        }

        [Test]
        public void BeamColor_IsRed()
        {
            UnityEngine.Color color = _settings.BeamColor;
            Assert.That(color.r, Is.GreaterThan(color.g).And.GreaterThan(color.b));
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
