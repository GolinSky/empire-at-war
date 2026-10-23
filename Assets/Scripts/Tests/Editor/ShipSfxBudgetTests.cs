using System;
using System.Reflection;
using System.Runtime.Serialization;
using EmpireAtWar.Services.Audio;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ShipSfxBudgetTests
    {
        private const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
        private ShipSfxData _data;
        private ShipSfxService _service;
        private Array _slots;
        private Type _slotType;

        [SetUp]
        public void SetUp()
        {
            _data = ScriptableObject.CreateInstance<ShipSfxData>();
            SetField(_data, "engineVoices", 2);
            SetField(_data, "perShipVoices", 2);
            // Slot selection is pure bookkeeping; no AudioSource or mixer is needed for these tests.
            _service = (ShipSfxService)FormatterServices.GetUninitializedObject(typeof(ShipSfxService));
            _slotType = typeof(ShipSfxService).GetNestedType("VoiceSlot", BindingFlags.NonPublic);
            _slots = Array.CreateInstance(_slotType, 4);
            SetField(_service, "_data", _data);
            SetField(_service, "_slots", _slots);
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_data);

        [Test]
        public void DifferentEngineProfilesShareTheEngineCapEvenWithFreeSlots()
        {
            AddSlot(0, Profile(10f), true);
            AddSlot(1, Profile(10f), true);
            Assert.That(FindSlot(Profile(10f), true), Is.EqualTo(-1));
        }

        [Test]
        public void StrongerEngineReplacesAnEngineRatherThanAQuieterWeapon()
        {
            AddSlot(0, Profile(10f), true);
            AddSlot(1, Profile(15f), true);
            AddSlot(2, Profile(1f), false);
            Assert.That(FindSlot(Profile(20f), true), Is.EqualTo(0));
        }

        [Test]
        public void FullEngineBudgetDoesNotBlockOtherSounds()
        {
            AddSlot(0, Profile(10f), true);
            AddSlot(1, Profile(10f), true);
            Assert.That(FindSlot(Profile(5f), false), Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void ShipsOwnWeaponsDoNotBlockItsEngine()
        {
            var ship = new ShipSfxHandle(100, default, null, null);
            AddSlot(0, Profile(60f), false, ship);
            AddSlot(1, Profile(60f), false, ship);
            Assert.That(FindSlot(ship, Profile(10f), true), Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void ShipAtItsVoiceCapReplacesAWeaponRatherThanItsEngine()
        {
            var ship = new ShipSfxHandle(100, default, null, null);
            AddSlot(0, Profile(1f), true, ship);
            AddSlot(1, Profile(30f), false, ship);
            AddSlot(2, Profile(30f), false, ship);
            Assert.That(FindSlot(ship, Profile(50f), false), Is.EqualTo(1));
        }

        [Test]
        public void AnnouncementBlocksUnitChatterBeforeItTouchesTheVoiceSource()
        {
            SetField(_service, "_interfaceSfx", new AnnouncingInterface());
            Assert.That(_service.TryPlayVoice(null), Is.False);
        }

        private void AddSlot(int index, SfxProfile sound, bool engine, ShipSfxHandle owner = null)
        {
            object slot = Activator.CreateInstance(_slotType);
            _slotType.GetField("Owner").SetValue(slot, owner ?? new ShipSfxHandle(index, default, null, null));
            _slotType.GetField("Sound").SetValue(slot, sound);
            _slotType.GetField("Gain").SetValue(slot, 1f);
            _slotType.GetField("IsEngine").SetValue(slot, engine);
            _slots.SetValue(slot, index);
        }

        private int FindSlot(SfxProfile sound, bool engine) =>
            FindSlot(new ShipSfxHandle(100, default, null, null), sound, engine);

        private int FindSlot(ShipSfxHandle ship, SfxProfile sound, bool engine) => (int)typeof(ShipSfxService)
            .GetMethod("FindSlot", PRIVATE_INSTANCE).Invoke(_service, new object[] { ship, sound, 1f, engine });

        private static SfxProfile Profile(float priority)
        {
            var profile = new SfxProfile();
            SetField(profile, "priority", priority);
            return profile;
        }

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, PRIVATE_INSTANCE).SetValue(target, value);

        private sealed class AnnouncingInterface : IInterfaceSfxService
        {
            public bool IsAnnouncing => true;
            public void PlayRejected() => throw new NotSupportedException();
            public void PlayAbilityTargetingCanceled() => throw new NotSupportedException();
            public void PlayAnnouncement(AudioClip clip) => throw new NotSupportedException();
        }
    }
}
