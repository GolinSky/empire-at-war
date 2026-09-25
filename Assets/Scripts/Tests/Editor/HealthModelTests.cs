using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Health;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class HealthModelTests
    {
        private const float HULL = 1000f;
        private const float SHIELDS = 500f;

        private DamageMatrixData _matrix;

        [SetUp]
        public void SetUp()
        {
            // Turbolaser: 1x vs capital, 1x vs shields. Ion: 0.1x vs capital, 3x vs shields. Torpedo: 2x vs capital, pierces.
            _matrix = ScriptableObject.CreateInstance<DamageMatrixData>();
            JsonUtility.FromJsonOverwrite(@"{""damageTypes"":[
                {""damageType"":2,""damage"":{""capital"":1.0},""accuracy"":{""capital"":1.0},""vsShield"":1.0,""shieldPiercing"":false},
                {""damageType"":4,""damage"":{""capital"":0.1},""accuracy"":{""capital"":1.0},""vsShield"":3.0,""shieldPiercing"":false},
                {""damageType"":6,""damage"":{""capital"":2.0},""accuracy"":{""capital"":1.0},""vsShield"":1.0,""shieldPiercing"":true}
            ]}", _matrix);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_matrix);
        }

        [Test]
        public void InitializeHardPoints_UsesHealthPerHardPointType()
        {
            HealthModel model = CreateModel(SHIELDS, out HardPointModel weapon, out HardPointModel engine,
                out HardPointModel shieldGenerator);

            Assert.That(weapon.Health, Is.EqualTo(100f));
            Assert.That(engine.Health, Is.EqualTo(200f));
            Assert.That(shieldGenerator.Health, Is.EqualTo(150f));
            Assert.That(model.Hull, Is.EqualTo(HULL));
            Assert.That(model.HasUnits, Is.True);
        }

        [Test]
        public void ApplyDamage_ShieldsAbsorbNonPiercingHits()
        {
            HealthModel model = CreateModel(SHIELDS, out HardPointModel weapon, out _, out _);

            model.ApplyDamage(100f, DamageType.Turbolaser, 0);

            Assert.That(model.Shields, Is.EqualTo(400f));
            Assert.That(model.Hull, Is.EqualTo(HULL));
            Assert.That(weapon.Health, Is.EqualTo(100f));
        }

        [Test]
        public void ApplyDamage_IonUsesShieldMultiplier()
        {
            HealthModel model = CreateModel(SHIELDS, out _, out _, out _);

            model.ApplyDamage(100f, DamageType.Ion, 0);

            Assert.That(model.Shields, Is.EqualTo(200f));
        }

        [Test]
        public void ApplyDamage_PiercingHitsHardPointAndHullThroughShields()
        {
            HealthModel model = CreateModel(SHIELDS, out HardPointModel weapon, out _, out _);

            model.ApplyDamage(40f, DamageType.ProtonTorpedo, 0);

            Assert.That(model.Shields, Is.EqualTo(SHIELDS));
            Assert.That(weapon.Health, Is.EqualTo(20f));
            Assert.That(model.Hull, Is.EqualTo(HULL - 80f * 0.5f));
        }

        [Test]
        public void ApplyDamage_DestroyedHardPointOnlyDamagesHull()
        {
            HealthModel model = CreateModel(0f, out HardPointModel weapon, out _, out _);

            model.ApplyDamage(300f, DamageType.Turbolaser, 0);
            model.ApplyDamage(100f, DamageType.Turbolaser, 0);

            Assert.That(weapon.IsDestroyed, Is.True);
            Assert.That(model.Hull, Is.EqualTo(HULL - 400f * 0.5f));
            Assert.That(model.IsDestroyed, Is.False);
        }

        [Test]
        public void ApplyDamage_ShieldGeneratorLossDropsShields()
        {
            HealthModel model = CreateModel(SHIELDS, out _, out _, out HardPointModel shieldGenerator);

            model.ApplyDamage(100f, DamageType.ProtonTorpedo, 2);

            Assert.That(shieldGenerator.IsDestroyed, Is.True);
            Assert.That(model.IsLostShieldGenerator, Is.True);
            Assert.That(model.Shields, Is.EqualTo(0f));
        }

        [Test]
        public void ApplyDamage_ShipDiesOnlyWhenHullIsEmpty()
        {
            HealthModel model = CreateModel(0f, out _, out HardPointModel engine, out _);
            bool destroyed = false;
            model.OnDestroy += () => destroyed = true;

            model.ApplyDamage(HULL - 1f, DamageType.Turbolaser, 1);
            Assert.That(destroyed, Is.False);

            model.ApplyDamage(1f, DamageType.Turbolaser, 1);
            Assert.That(engine.IsDestroyed, Is.True);
            Assert.That(destroyed, Is.True);
            Assert.That(model.HasUnits, Is.False);
        }

        [Test]
        public void HasLiveHardPoints_FalseOnceEveryHardPointIsDestroyed()
        {
            HealthModel model = CreateModel(0f, out _, out _, out _);

            model.ApplyDamage(100f, DamageType.Turbolaser, 0);
            model.ApplyDamage(200f, DamageType.Turbolaser, 1);
            model.ApplyDamage(150f, DamageType.Turbolaser, 2);

            Assert.That(model.HasLiveHardPoints, Is.False);
            Assert.That(model.HasUnits, Is.True);
        }

        private HealthModel CreateModel(float shields, out HardPointModel weapon, out HardPointModel engine,
            out HardPointModel shieldGenerator)
        {
            HealthModel model = new HealthModel(new HealthDataStub(shields), _matrix, new CombatModifiers());
            weapon = new HardPointModel(0, HardPointType.Weapon);
            engine = new HardPointModel(1, HardPointType.Engines);
            shieldGenerator = new HardPointModel(2, HardPointType.ShieldGenerator);
            model.InitializeHardPoints(new[] { weapon, engine, shieldGenerator });
            return model;
        }

        private sealed class HealthDataStub : IHealthData
        {
            public HealthDataStub(float shields)
            {
                Shields = shields;
            }

            public ShipClass ShipClass => ShipClass.Capital;
            public float Hull => HULL;
            public float Shields { get; }
            public float ShieldRegenerateValue => 0f;
            public float ShieldRegenerateDelay => 0f;

            public IReadOnlyList<HardPointHealth> HardPointHealth { get; } = new[]
            {
                CreateHealth(HardPointType.Weapon, 100f, 0.5f),
                CreateHealth(HardPointType.Engines, 200f, 1f),
                CreateHealth(HardPointType.ShieldGenerator, 150f, 1f),
            };

            private static HardPointHealth CreateHealth(HardPointType type, float health, float hullMultiplier) =>
                JsonUtility.FromJson<HardPointHealth>(FormattableString.Invariant(
                    $"{{\"hardPointType\":{(int)type},\"health\":{health},\"hullDamageMultiplier\":{hullMultiplier}}}"));
        }
    }
}
