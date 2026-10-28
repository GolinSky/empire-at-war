using System;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.Serialization;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Audio;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Audio")]
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
            _slotType.GetField("EndsAt").SetValue(slot, 10f);
            _slotType.GetField("VolumeScale").SetValue(slot, 1f);
            _slotType.GetField("IsEngine").SetValue(slot, engine);
            _slots.SetValue(slot, index);
        }

        [Test]
        public void LocalFeedbackBypassesWeaponBurstBudgetAndCameraAttenuation()
        {
            var root = new GameObject("Local feedback test");
            var clip = AudioClip.Create("Shield cue", 44100, 1, 44100, false);
            try
            {
                var sources = root.AddComponent<ShipSfxSources>();
                var audioSources = new AudioSource[4];
                for (int i = 0; i < audioSources.Length; i++) audioSources[i] = root.AddComponent<AudioSource>();
                SetField(sources, "sfx", audioSources);
                SetField(sources, "voice", audioSources[0]);
                SetField(_service, "_sources", sources);
                SetField(_service, "_audioService", new RecordingAudio());
                SetField(_service, "_localPlayer", TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()));
                SetField(_service, "_cooldowns", new Dictionary<SfxProfile, float>());
                var starts = new Queue<float>(new[] { 0f, 0f, 0f });
                SetField(_service, "_starts", starts);
                SetField(_service, "_timeFrame", Time.frameCount);
                var sound = Profile(150f);
                SetField(sound, "clip", clip);
                SetField(sound, "localFeedback", true);
                SetField(sound, "cooldown", 0.15f);
                var ship = new ShipSfxHandle(100, TestPlayers.Human, new HealthStub(), null);
                Assert.That(_service.TryPlayOneShot(ship, sound, new Vector3(999f, 0f, -999f)), Is.True);
                Assert.That(starts.Count, Is.EqualTo(3), "Feedback must not consume weapon burst slots.");
                Assert.That(_service.TryPlayOneShot(ship, sound, Vector3.zero), Is.False, "Feedback still obeys its own cooldown.");
                object slot = _slots.GetValue(3);
                Assert.That(_slotType.GetField("Gain").GetValue(slot), Is.EqualTo(1f));
                Assert.That(_slotType.GetField("Pan").GetValue(slot), Is.EqualTo(0f));
                Assert.That(audioSources[3].priority, Is.LessThan(128));
                Assert.That(audioSources[3].spatialBlend, Is.Zero);
                var weapon = Profile(30f);
                SetField(weapon, "volume", 1f);
                AddSlot(0, weapon, false);
                AddSlot(1, weapon, false);
                SetField(_service, "_interfaceSfx", new AnnouncingInterface { IsAnnouncing = false });
                SetField(_service, "_hasDuckState", true);
                typeof(ShipSfxService).GetMethod("ApplyMix", PRIVATE_INSTANCE).Invoke(_service, null);
                Assert.That(audioSources[3].volume, Is.EqualTo(sound.Volume).Within(0.001f),
                    "Battle mix normalization must not reduce local ability feedback.");
                Assert.That(audioSources[0].volume, Is.EqualTo(0.5f).Within(0.001f));
                var enemy = new ShipSfxHandle(101, TestPlayers.Enemy, new HealthStub(), null);
                Assert.That(_service.TryPlayOneShot(enemy, sound, Vector3.zero), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        private sealed class RecordingAudio : Service, IAudioService
        {
            public void Play(AudioSource source, AudioClip clip, float volume, bool loop) { source.clip = clip; }
            public void Stop(AudioSource source) { }
            public void PlayOneShot(AudioSource source, AudioClip clip, float volume) => throw new NotSupportedException();
            public void Pause(AudioSource source) => throw new NotSupportedException();
            public void UnPause(AudioSource source) => throw new NotSupportedException();
            public void SetGamePaused(bool paused) => throw new NotSupportedException();
        }

        private sealed class HealthStub : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
            public ShipClass ShipClass => ShipClass.Cruiser;
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1f;
            public float HullPercentage => 1f;
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public bool HasLiveHardPoints => false;
            public bool HasShields => false;
            public PlayerId Owner => TestPlayers.Human;
            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) => Array.Empty<IHardPointModel>();
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
            public bool IsAnnouncing { get; set; } = true;
            public void PlayRejected() => throw new NotSupportedException();
            public void PlayAbilityTargetingCanceled() => throw new NotSupportedException();
            public void PlayAnnouncement(AudioClip clip) => throw new NotSupportedException();
            public bool TryPlayAnnouncement(AudioClip clip, AnnouncementPriority priority) => throw new NotSupportedException();
            public bool TryPlayStationUpgrade(AudioClip clip, AudioClip newConstruction) => throw new NotSupportedException();
        }
    }
}
