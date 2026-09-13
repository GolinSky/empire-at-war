using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Health;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class HealthModelTests
    {
        [Test]
        public void InitializeHardPoints_DistributesArmorAndNormalizesHealthPercentages()
        {
            HealthModel model = new HealthModel(new HealthDataStub(100f), new DamageCalculatorStub());
            HardPointModel weapon = new HardPointModel(0, HardPointType.Weapon);
            HardPointModel engine = new HardPointModel(1, HardPointType.Engines);
            HardPointModel shieldGenerator = new HardPointModel(2, HardPointType.ShieldGenerator);

            model.InitializeHardPoints(new[] { weapon, engine, shieldGenerator });

            Assert.That(weapon.Health, Is.EqualTo(80f));
            Assert.That(engine.Health, Is.EqualTo(10f));
            Assert.That(shieldGenerator.Health, Is.EqualTo(10f));
            Assert.That(weapon.HealthPercentage, Is.EqualTo(1f));
            Assert.That(engine.HealthPercentage, Is.EqualTo(1f));
        }

        [Test]
        public void InitializeHardPoints_CopiesInputCollection()
        {
            HealthModel model = new HealthModel(new HealthDataStub(100f), new DamageCalculatorStub());
            List<HardPointModel> hardPoints = new List<HardPointModel>
            {
                new HardPointModel(0, HardPointType.Weapon),
            };

            model.InitializeHardPoints(hardPoints);
            hardPoints.Clear();

            Assert.That(model.HardPointModels, Has.Length.EqualTo(1));
            Assert.That(model.HasUnits, Is.True);
        }

        private sealed class HealthDataStub : IHealthData
        {
            public HealthDataStub(float armor)
            {
                Armor = armor;
            }

            public float Armor { get; }
            public float Dexterity => 0f;
            public float Shields => 0f;
            public float ShieldRegenerateValue => 0f;
            public float ShieldRegenerateDelay => 0f;
        }

        private sealed class DamageCalculatorStub : IDamageCalculator
        {
            public DamageData GetDamage(
                WeaponType weaponType,
                IHealthState healthState,
                bool isMoving,
                float damage)
            {
                return new DamageData(0f, 0f);
            }
        }
    }
}
