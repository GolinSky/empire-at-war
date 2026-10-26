using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.Ship.EntityFacades.Health;
using EmpireAtWar.Models.Health;
using EmpireAtWar.ViewComponents.Health;
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
        public void PointDefense_WhileCloakedPreservesMissileAndCanFireAfterDecloaking()
        {
            HealthModel model = CreateModel(SHIELDS, out _, out _, out _);
            CombatModifiers modifiers = new CombatModifiers();
            IncomingMissileRegistry missiles = new IncomingMissileRegistry();
            GameObject mountObject = new GameObject("PointDefense");
            try
            {
                const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
                HealthComponent health = mountObject.AddComponent<HealthComponent>();
                typeof(HealthComponent).GetMethod("Construct", PRIVATE_INSTANCE)
                    .Invoke(health, new object[] { model, modifiers, mountObject.transform, TestPlayers.Human });
                IncomingMissile missile = missiles.Launch(health, Vector3.forward, 100f);
                MissileInterceptorHardPoint mount = mountObject.AddComponent<MissileInterceptorHardPoint>();
                LineRenderer laser = mountObject.AddComponent<LineRenderer>();
                laser.positionCount = 2;
                laser.enabled = false;
                typeof(MissileInterceptorHardPoint).GetField("laser", PRIVATE_INSTANCE).SetValue(mount, laser);
                typeof(MissileInterceptorHardPoint).GetField("interceptChance", PRIVATE_INSTANCE).SetValue(mount, 1f);
                typeof(MissileInterceptorHardPoint).GetMethod("Construct", PRIVATE_INSTANCE)
                    .Invoke(mount, new object[] { missiles, health, modifiers });
                MethodInfo update = typeof(MissileInterceptorHardPoint).GetMethod("Update", PRIVATE_INSTANCE);

                modifiers.SetCloaked(true);
                update.Invoke(mount, null);
                Assert.That(missile.IsIntercepted, Is.False);
                Assert.That(laser.enabled, Is.False);

                modifiers.SetCloaked(false);
                update.Invoke(mount, null);
                Assert.That(missile.IsIntercepted, Is.True);
                Assert.That(laser.enabled, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mountObject);
            }
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
        public void DamageFacadeNotifiesOnlyDamageAndSuppressesRestoreAndZeroDamage()
        {
            HealthModel model = CreateModel(SHIELDS, out _, out _, out _);
            GameObject owner = new GameObject("DamageFacadeTest");
            try
            {
                HealthComponent health = owner.AddComponent<HealthComponent>();
                typeof(HealthComponent).GetMethod("Construct", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(health, new object[] { model, new CombatModifiers(), owner.transform, TestPlayers.Human });
                HealthFacade facade = new HealthFacade(health);
                int damageEvents = 0;
                facade.Damaged += () => damageEvents++;

                model.Restore(0.5f, 0.5f, new[] { 1f, 1f, 1f });
                facade.ApplyDamage(0f, DamageType.Turbolaser, 0);
                Assert.That(damageEvents, Is.Zero);
                facade.ApplyDamage(10f, DamageType.Turbolaser, 0);

                Assert.That(damageEvents, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
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
        public void ApplyDamage_DestroyedHardPointSpreadsDamageOverLiveHardPoints()
        {
            HealthModel model = CreateModel(0f, out HardPointModel weapon, out HardPointModel engine,
                out HardPointModel shieldGenerator);

            model.ApplyDamage(300f, DamageType.Turbolaser, 0);
            model.ApplyDamage(100f, DamageType.Turbolaser, 0);

            Assert.That(weapon.IsDestroyed, Is.True);
            Assert.That(engine.Health, Is.EqualTo(150f));
            Assert.That(shieldGenerator.Health, Is.EqualTo(100f));
            Assert.That(model.Hull, Is.EqualTo(HULL - 300f * 0.5f - 50f - 50f));
            Assert.That(model.IsDestroyed, Is.False);
        }

        [Test]
        public void ApplyDamage_DestroyedHardPointOnlyDamagesHullOnceAllAreDestroyed()
        {
            HealthModel model = CreateModel(0f, out _, out _, out _);
            model.ApplyDamage(100f, DamageType.Turbolaser, 0);
            model.ApplyDamage(200f, DamageType.Turbolaser, 1);
            model.ApplyDamage(150f, DamageType.Turbolaser, 2);
            float hull = model.Hull;

            model.ApplyDamage(100f, DamageType.Turbolaser, 0);

            Assert.That(model.Hull, Is.EqualTo(hull - 100f * 0.5f));
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

            model.ApplyDamage(1f, DamageType.Turbolaser, 2);
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

        [Test]
        public void Restore_AppliesSavedFractions()
        {
            HealthModel model = CreateModel(SHIELDS, out HardPointModel weapon, out HardPointModel engine,
                out HardPointModel shieldGenerator);

            model.Restore(0.25f, 0.5f, new[] { 0.5f, 0f, 1f });

            Assert.That(model.Hull, Is.EqualTo(HULL * 0.25f));
            Assert.That(model.Shields, Is.EqualTo(SHIELDS * 0.5f));
            Assert.That(weapon.Health, Is.EqualTo(50f));
            Assert.That(engine.IsDestroyed, Is.True);
            Assert.That(shieldGenerator.Health, Is.EqualTo(150f));
            Assert.That(model.IsLostShieldGenerator, Is.False);
        }

        [Test]
        public void Restore_DestroyedShieldGeneratorDropsShields()
        {
            HealthModel model = CreateModel(SHIELDS, out _, out _, out _);

            model.Restore(1f, 1f, new[] { 1f, 1f, 0f });

            Assert.That(model.IsLostShieldGenerator, Is.True);
            Assert.That(model.Shields, Is.Zero);
        }

        [Test]
        public void Restore_HardPointCountMismatch_Throws()
        {
            HealthModel model = CreateModel(SHIELDS, out _, out _, out _);

            Assert.Throws<InvalidOperationException>(() => model.Restore(1f, 1f, new[] { 1f }));
        }

        [Test]
        public void RegenerateShields_AfterDeath_DoesNotRestoreShieldsOrNotify()
        {
            HealthModel model = CreateModel(SHIELDS, out _, out _, out _);
            model.ApplyDamage(SHIELDS, DamageType.Turbolaser, 0);
            model.ApplyDamage(HULL, DamageType.Turbolaser, 1);
            int changes = 0;
            model.OnValueChanged += () => changes++;

            model.RegenerateShields(100f);

            Assert.That(model.IsDestroyed, Is.True);
            Assert.That(model.Shields, Is.Zero);
            Assert.That(changes, Is.Zero);
        }

        private HealthModel CreateModel(float shields, out HardPointModel weapon, out HardPointModel engine,
            out HardPointModel shieldGenerator)
        {
            HealthModel model = new HealthModel(new HealthDataStub(shields), _matrix, new CombatModifiers());
            weapon = new HardPointModel(id: 0, hardPointType: HardPointType.Weapon);
            engine = new HardPointModel(id: 1, hardPointType: HardPointType.Engines);
            shieldGenerator = new HardPointModel(id: 2, hardPointType: HardPointType.ShieldGenerator);
            model.InitializeHardPoints(new[] { weapon, engine, shieldGenerator });
            return model;
        }

        private sealed class HealthDataStub : IHealthData
        {
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

            public HealthDataStub(float shields)
            {
                Shields = shields;
            }

            private static HardPointHealth CreateHealth(HardPointType type, float health, float hullMultiplier) =>
                JsonUtility.FromJson<HardPointHealth>(FormattableString.Invariant(
                    $"{{\"hardPointType\":{(int)type},\"health\":{health},\"hullDamageMultiplier\":{hullMultiplier}}}"));
        }
    }
}
