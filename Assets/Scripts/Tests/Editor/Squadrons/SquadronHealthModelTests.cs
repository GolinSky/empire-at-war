using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Models.Health;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Squadrons")]
    public sealed class SquadronHealthModelTests
    {
        private const float MEMBER_HULL = 80f;
        private const float MEMBER_SHIELDS = 20f;

        private const int MEMBER_COUNT = 3;

        private DamageMatrixData _matrix;

        [SetUp]
        public void SetUp()
        {
            // Laser: 1x vs fighters, 0.5x vs shields. Torpedo: 1x vs fighters, pierces shields.
            _matrix = ScriptableObject.CreateInstance<DamageMatrixData>();
            JsonUtility.FromJsonOverwrite(@"{""damageTypes"":[
                {""damageType"":0,""damage"":{""fighter"":1.0},""accuracy"":{""fighter"":1.0},""vsShield"":0.5,""shieldPiercing"":false},
                {""damageType"":6,""damage"":{""fighter"":1.0},""accuracy"":{""fighter"":1.0},""vsShield"":1.0,""shieldPiercing"":true}
            ]}", _matrix);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_matrix);
        }

        [Test]
        public void ApplyDamage_ShieldsProtectOnlyTheHitFighter()
        {
            SquadronHealthModel model = CreateModel(out HardPointModel[] members);

            model.ApplyDamage(20f, DamageType.Laser, 1);

            Assert.That(model.GetMemberShields(1), Is.EqualTo(10f));
            Assert.That(model.GetMemberShields(0), Is.EqualTo(MEMBER_SHIELDS));
            Assert.That(members[1].Health, Is.EqualTo(MEMBER_HULL));
            Assert.That(model.Shields, Is.EqualTo(MEMBER_SHIELDS * MEMBER_COUNT - 10f));
        }

        [Test]
        public void ApplyDamage_SquadronShieldBonusDoesNotChangeShipShieldMultiplier()
        {
            JsonUtility.FromJsonOverwrite(@"{""damageTypes"":[
                {""damageType"":0,""damage"":{""fighter"":2},""vsShield"":0.5,""squadronShieldMultiplier"":2}
            ]}", _matrix);
            SquadronHealthModel model = CreateModel(out HardPointModel[] members);

            model.ApplyDamage(10f, DamageType.Laser, 1);

            Assert.That(model.GetMemberShields(1), Is.EqualTo(10f));
            Assert.That(model.GetMemberShields(0), Is.EqualTo(MEMBER_SHIELDS));
            Assert.That(members[1].Health, Is.EqualTo(MEMBER_HULL));
            Assert.That(_matrix.GetShieldMultiplier(DamageType.Laser), Is.EqualTo(0.5f));
        }

        [Test]
        public void ApplyDamage_KillsOnlyTheHitFighter()
        {
            SquadronHealthModel model = CreateModel(out HardPointModel[] members);

            model.ApplyDamage(MEMBER_HULL, DamageType.ProtonTorpedo, 2);

            Assert.That(members[2].IsDestroyed, Is.True);
            Assert.That(members[0].IsDestroyed, Is.False);
            Assert.That(model.AliveCount, Is.EqualTo(MEMBER_COUNT - 1));
            Assert.That(model.IsDestroyed, Is.False);
            Assert.That(model.Hull, Is.EqualTo(MEMBER_HULL * (MEMBER_COUNT - 1)));
        }

        [Test]
        public void ApplyDamage_DestroysSquadronWithLastFighter()
        {
            SquadronHealthModel model = CreateModel(out _);
            int destroyEvents = 0;
            model.OnDestroy += () => destroyEvents++;

            for (int i = 0; i < MEMBER_COUNT; i++)
            {
                model.ApplyDamage(MEMBER_HULL, DamageType.ProtonTorpedo, i);
            }

            Assert.That(model.IsDestroyed, Is.True);
            Assert.That(model.HasUnits, Is.False);
            Assert.That(destroyEvents, Is.EqualTo(1));
        }

        [Test]
        public void ApplyDamage_IgnoresHitsOnDeadFighter()
        {
            SquadronHealthModel model = CreateModel(out _);
            model.ApplyDamage(MEMBER_HULL, DamageType.ProtonTorpedo, 0);
            float hull = model.Hull;

            model.ApplyDamage(MEMBER_HULL, DamageType.ProtonTorpedo, 0);

            Assert.That(model.Hull, Is.EqualTo(hull));
        }

        [Test]
        public void RegenerateShields_SkipsDeadFightersAndCapsAtMaximum()
        {
            SquadronHealthModel model = CreateModel(out _);
            model.ApplyDamage(MEMBER_HULL, DamageType.ProtonTorpedo, 0);
            model.ApplyDamage(20f, DamageType.Laser, 1);

            model.RegenerateShields(100f);

            Assert.That(model.GetMemberShields(0), Is.EqualTo(0f));
            Assert.That(model.GetMemberShields(1), Is.EqualTo(MEMBER_SHIELDS));
        }

        [Test]
        public void InitializeMembers_AfterDestruction_RestoresLiveSquadron()
        {
            SquadronHealthModel model = CreateModel(out _);
            for (int i = 0; i < MEMBER_COUNT; i++)
            {
                model.ApplyDamage(MEMBER_HULL, DamageType.ProtonTorpedo, i);
            }

            model.InitializeMembers(CreateMembers());

            Assert.That(model.IsDestroyed, Is.False);
            Assert.That(model.HasUnits, Is.True);
            Assert.That(model.AliveCount, Is.EqualTo(MEMBER_COUNT));
            Assert.That(model.Hull, Is.EqualTo(MEMBER_HULL * MEMBER_COUNT));
            Assert.That(model.Shields, Is.EqualTo(MEMBER_SHIELDS * MEMBER_COUNT));
        }

        [Test]
        public void InitializeMembers_RefreshesModifiersChangedWhileDespawned()
        {
            CombatModifiers modifiers = new CombatModifiers();
            modifiers.SetFleetCommand(true);
            SquadronHealthModel model = new SquadronHealthModel(new TestHealthData(), _matrix, modifiers);
            model.InitializeMembers(CreateMembers());
            for (int i = 0; i < MEMBER_COUNT; i++)
                model.ApplyDamage(MEMBER_HULL * 10f, DamageType.ProtonTorpedo, i);

            modifiers.SetFleetCommand(false);
            model.InitializeMembers(CreateMembers());

            Assert.That(model.Hull, Is.EqualTo(MEMBER_HULL * MEMBER_COUNT));
            Assert.That(model.MaxHull, Is.EqualTo(MEMBER_HULL * MEMBER_COUNT));
            Assert.That(model.Shields, Is.EqualTo(MEMBER_SHIELDS * MEMBER_COUNT));
            Assert.That(model.MaxShields, Is.EqualTo(MEMBER_SHIELDS * MEMBER_COUNT));
        }

        [Test]
        public void InitializeMembers_AfterSuppressedDeath_DoesNotCarrySuppressionIntoRespawn()
        {
            CombatModifiers modifiers = new CombatModifiers();
            SquadronHealthModel model = new SquadronHealthModel(new TestHealthData(), _matrix, modifiers);
            model.InitializeMembers(CreateMembers());
            modifiers.AddShieldSuppression();
            model.SetShieldSuppressed(true);
            for (int i = 0; i < MEMBER_COUNT; i++)
                model.ApplyDamage(MEMBER_HULL, DamageType.ProtonTorpedo, i);
            modifiers.RemoveShieldSuppression();
            // The health component unsubscribes before the hazard service releases a dead entity.
            model.InitializeMembers(CreateMembers());
            Assert.That(model.IsShieldSuppressed, Is.False);
            Assert.That(model.Shields, Is.EqualTo(MEMBER_SHIELDS * MEMBER_COUNT));
        }

        private static HardPointModel[] CreateMembers()
        {
            HardPointModel[] members = new HardPointModel[MEMBER_COUNT];
            for (int i = 0; i < MEMBER_COUNT; i++)
            {
                members[i] = new HardPointModel(id: i, hardPointType: HardPointType.Any);
            }

            return members;
        }

        [Test]
        public void Restore_AppliesEachFighterAndKeepsDeadFightersDead()
        {
            SquadronHealthModel model = CreateModel(out HardPointModel[] members);

            model.Restore(new[] { 1f, 0.5f, 0f }, new[] { 0.25f, 1f, 1f });

            Assert.That(members[1].Health, Is.EqualTo(MEMBER_HULL * 0.5f));
            Assert.That(members[2].IsDestroyed, Is.True);
            Assert.That(model.GetMemberShields(0), Is.EqualTo(MEMBER_SHIELDS * 0.25f));
            Assert.That(model.GetMemberShields(2), Is.Zero);
            Assert.That(model.AliveCount, Is.EqualTo(2));
            Assert.That(model.GetMemberShieldPercentage(0), Is.EqualTo(0.25f));
        }

        private SquadronHealthModel CreateModel(out HardPointModel[] members)
        {
            members = CreateMembers();
            SquadronHealthModel model = new SquadronHealthModel(new TestHealthData(), _matrix, new CombatModifiers());
            model.InitializeMembers(members);
            return model;
        }

        [Test]
        public void ShieldSuppression_AffectsEveryMemberAndAllowsRecoveryFromZero()
        {
            SquadronHealthModel model = CreateModel(out HardPointModel[] members);
            model.SetShieldSuppressed(true);
            model.RegenerateShields(100f);
            Assert.That(model.Shields, Is.Zero);
            model.ApplyDamage(5f, DamageType.Laser, 0);
            Assert.That(members[0].Health, Is.LessThan(members[0].MaxHealth));
            model.SetShieldSuppressed(false);
            Assert.That(model.Shields, Is.Zero);
            model.RegenerateShields(1f);
            Assert.That(model.GetMemberShields(0), Is.EqualTo(1f));
            Assert.That(model.GetMemberShields(1), Is.EqualTo(1f));
        }

        private sealed class TestHealthData : ISquadronHealthData
        {
            public ShipClass ShipClass => ShipClass.Fighter;
            public float MemberHull => MEMBER_HULL;
            public float MemberShields => MEMBER_SHIELDS;
            public float ShieldRegenerateValue => 1f;
            public float ShieldRegenerateDelay => 1f;
            public float LaserShieldDamageMultiplier => 1f;
        }
    }
}
