using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Vision;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Combat")]
    public sealed class WeaponFiringRevealTests
    {
        [TestCase(false, false, false, true, ShipClass.Capital)]
        [TestCase(true, false, false, false, ShipClass.Capital)]
        [TestCase(false, true, false, false, ShipClass.Capital)]
        [TestCase(false, false, true, false, ShipClass.Capital)]
        [TestCase(false, false, false, true, ShipClass.Structure)]
        [TestCase(false, false, false, true, ShipClass.Fighter)]
        [TestCase(false, false, false, true, ShipClass.Bomber)]
        [TestCase(false, false, false, true, ShipClass.Interceptor)]
        [TestCase(false, false, false, true, ShipClass.HeroStrikecraft)]
        public void EmittedShot_RevealsOnlyLivingHostileTargetOnAuthority(bool replica, bool allied, bool destroyed,
            bool expected, ShipClass attackerClass)
        {
            var root = new GameObject("Firing reveal");
            try
            {
                float clock = 0f;
                var vision = new VisionService(TestPlayers.CreateTeamGame(), true, () => clock, 7f);
                WeaponComponent weapon = root.AddComponent<WeaponComponent>();
                Set(weapon, "_visionService", vision);
                Set(weapon, "_playerRoster", TestPlayers.CreateTeamGame());
                Set(weapon, "_owner", TestPlayers.Enemy);
                Set(weapon, "_entityId", 42L);
                Set(weapon, "_attackerClass", attackerClass);
                Set(weapon, "_followsHost", replica);
                var health = new Health { IsDestroyed = destroyed };
                PlayerId victim = allied ? TestPlayers.Enemy : TestPlayers.Human;
                var entity = new Entity(health, null, Array.Empty<IEntityFacade>(), victim, 7, null, null);
                var attack = new AttackData(health, null, HardPointType.Any, entity);
                var reveals = new List<VisionReveal>();

                Emit(weapon, attack);
                vision.CopyActiveReveals(reveals);
                Assert.That(reveals.Count, Is.EqualTo(expected ? 1 : 0));
                if (!expected) return;
                Assert.That(reveals[0].Viewer, Is.EqualTo(victim));
                Assert.That(reveals[0].EntityId, Is.EqualTo(42));

                clock = 4f;
                Emit(weapon, attack);
                clock = 5f;
                vision.Tick();
                vision.CopyActiveReveals(reveals);
                Assert.That(reveals.Count, Is.EqualTo(1), "The last shot refreshes the configured window.");
                clock = 10f;
                vision.Tick();
                vision.CopyActiveReveals(reveals);
                Assert.That(reveals.Count, Is.EqualTo(1), "The configured duration replaces the former five-second constant.");
                clock = 11f;
                vision.Tick();
                vision.CopyActiveReveals(reveals);
                Assert.That(reveals, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void Set(WeaponComponent weapon, string name, object value) =>
            typeof(WeaponComponent).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(weapon, value);

        private static void Emit(WeaponComponent weapon, AttackData attack) =>
            typeof(WeaponComponent).GetMethod("OnAttackEmitted", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(weapon, new object[] { attack });

        private sealed class Health : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
            public ShipClass ShipClass => ShipClass.Capital;
            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 100f;
            public float HullPercentage => 1f;
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed { get; set; }
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public bool HasLiveHardPoints => true;
            public bool HasShields => false;
            public PlayerId Owner => TestPlayers.Human;
            public IHardPointModel[] GetShipUnits(HardPointType type) => Array.Empty<IHardPointModel>();
        }
    }
}
