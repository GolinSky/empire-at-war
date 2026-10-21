using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.ShipUi;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.ShipAbilities;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EmpireAtWar.Tests.Editor
{
    [TestFixture(ShipAbilityId.TractorBeam, HardPointType.TractorBeam)]
    [TestFixture(ShipAbilityId.DauntlessSensorJamming, HardPointType.SensorArray)]
    [TestFixture(ShipAbilityId.FighterCoordination, HardPointType.CommandArray)]
    public sealed class ShipAbilityPerformanceTests
    {
        private readonly ShipAbilityId _abilityId;
        private readonly HardPointType _requiredType;

        public ShipAbilityPerformanceTests(ShipAbilityId abilityId, HardPointType requiredType)
        {
            _abilityId = abilityId;
            _requiredType = requiredType;
        }

        [Test]
        public void Availability_TracksSubsystemDestructionAndRestoration()
        {
            HardPointModel point = CreateHardPoint(_requiredType, false);
            TestCaster caster = CreateCaster(point);
            Assert.That(caster.Slots[0].CanPress, Is.True);
            point.ApplyDamage(100f);
            Assert.That(caster.Slots[0].CanPress, Is.False);
            point.Restore(1f);
            Assert.That(caster.Slots[0].CanPress, Is.True);
            point.Uninstall();
            Assert.That(caster.Slots[0].CanPress, Is.False);
        }

        [Test]
        public void RepeatedAvailabilityChecks_DoNotAllocate()
        {
            ShipAbilitySlot slot = CreateCaster(CreateHardPoint(_requiredType, false)).Slots[0];
            for (int i = 0; i < 100; i++) _ = slot.CanPress;
            GcAllocationAssert.DoesNotAllocate(() =>
            {
                for (int i = 0; i < 1000; i++) _ = slot.CanPress;
            });
        }

        [Test]
        public void CooldownLabel_ReusesTextUntilDisplayedSecondOrTargetingChanges()
        {
            var root = new GameObject("Ability cooldown", typeof(RectTransform));
            root.SetActive(false);
            var active = new GameObject("Active");
            var targeting = new GameObject("Targeting");
            var labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.SetActive(false);
            try
            {
                ShipAbilitySlot slot = CreateCaster(CreateHardPoint(_requiredType, false)).Slots[0];
                typeof(ShipAbilitySlot).GetMethod("Restore", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(slot, new object[] { ShipAbilityState.Recovering, 1.8f });
                var view = root.AddComponent<ShipAbilityButtonUi>();
                var label = labelObject.AddComponent<TextMeshProUGUI>();
                SetViewField(view, "_slots", new[] { slot });
                SetViewField(view, "button", root.AddComponent<Button>());
                SetViewField(view, "cooldownFill", root.AddComponent<Image>());
                SetViewField(view, "activeHighlight", active);
                SetViewField(view, "targetingHighlight", targeting);
                SetViewField(view, "cooldownText", label);
                var update = (Action)Delegate.CreateDelegate(typeof(Action), view,
                    typeof(ShipAbilityButtonUi).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic));
                update();
                string original = label.text;
                Assert.That(original, Is.EqualTo("2s"));
                update();
                Assert.That(label.text, Is.SameAs(original));
                for (int i = 0; i < 100; i++) update();
                GcAllocationAssert.DoesNotAllocate(() =>
                {
                    for (int i = 0; i < 1000; i++) update();
                });
                typeof(ShipAbilitySlot).GetMethod("Elapse", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(slot, new object[] { 1f });
                update();
                Assert.That(label.text, Is.EqualTo("1s"));
                view.SetWaiting(true);
                update();
                Assert.That(label.text, Is.EqualTo("TARGET"));
                view.SetWaiting(false);
                update();
                Assert.That(label.text, Is.EqualTo("1s"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(active);
                UnityEngine.Object.DestroyImmediate(targeting);
                UnityEngine.Object.DestroyImmediate(labelObject);
            }
        }

        private static void SetViewField(ShipAbilityButtonUi view, string name, object value) =>
            typeof(ShipAbilityButtonUi).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(view, value);

        private TestCaster CreateCaster(params HardPointModel[] hardPoints)
        {
            ShipAbilityDefinition definition = new ShipAbilityDefinition();
            typeof(ShipAbilityDefinition).GetField("requiresEnemyTarget",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(definition, true);
            return new TestCaster(_abilityId, definition, hardPoints);
        }

        private sealed class TestCaster : IShipAbilityFacade, IEntity
        {
            public IReadOnlyList<ShipAbilitySlot> Slots { get; }
            public CombatModifiers Modifiers { get; } = new CombatModifiers();
            public Vector3 WorldPosition => Vector3.zero;
            public IEntity Entity => this;
            public IHealthModelObserver Health { get; }
            public float RadarRange => 100f;
            public long Id => 1;
            public IHealthModelObserver HealthModel => Health;
            public PlayerId Owner => TestPlayers.Human;

            public TestCaster(ShipAbilityId id, ShipAbilityDefinition definition, HardPointModel[] hardPoints)
            {
                Health = new TestHealth(hardPoints);
                Slots = new[] { new ShipAbilitySlot(this, definition, id) };
            }

            public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade =>
                this is TFacade facade ? facade : throw new InvalidOperationException();

            public bool TryGetFacade<TFacade>(out TFacade facade) where TFacade : IEntityFacade
            {
                if (this is TFacade matching) { facade = matching; return true; }
                facade = default;
                return false;
            }
        }

        private sealed class TestHealth : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
            public ShipClass ShipClass => ShipClass.Capital;
            public HardPointModel[] HardPointModels { get; }
            public float Hull => 1f;
            public float HullPercentage => 1f;
            public float Shields => 1f;
            public float ShieldPercentage => 1f;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public bool HasLiveHardPoints => true;
            public bool HasShields => true;
            public PlayerId Owner => TestPlayers.Human;

            public TestHealth(HardPointModel[] hardPoints) { HardPointModels = hardPoints; }

            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) =>
                throw new InvalidOperationException("Ability availability must not allocate targeting snapshots.");
        }

        private static HardPointModel CreateHardPoint(HardPointType type, bool destroyed)
        {
            var point = new HardPointModel(type, 0);
            point.SetHealth(destroyed ? 0f : 100f, 1f);
            return point;
        }
    }
}
