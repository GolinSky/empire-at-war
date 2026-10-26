using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.SuperWeapons.Controller;
using EmpireAtWar.Entities.SuperWeapons.Ui;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.SuperWeapons;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class SuperWeaponAudioTests
    {
        [Test]
        public void InterfaceAudio_InitializesBeforePlayerScopesAndReleasesSourcesAndSubscriptions()
        {
            InterfaceSfxData data = ScriptableObject.CreateInstance<InterfaceSfxData>();
            ShipSfxData shipData = ScriptableObject.CreateInstance<ShipSfxData>();
            GameObject root = new GameObject("Interface lifecycle test");
            try
            {
                ShipSfxSources sources = root.AddComponent<ShipSfxSources>();
                AudioSource voice = root.AddComponent<AudioSource>();
                SetField(sources, "voice", voice);
                ShipAbilityService abilities = new ShipAbilityService(null, TestPlayers.CreateDuel());
                InterfaceSfxService service = new InterfaceSfxService(data, shipData, sources, abilities,
                    TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()));
                LifecycleOrderAttribute order = typeof(InterfaceSfxService).GetCustomAttribute<LifecycleOrderAttribute>();
                Assert.That(order.Initialize, Is.LessThan(LifecycleOrders.PLAYER_SCOPES_INITIALIZE));
                service.Initialize();
                Assert.That(service.IsAnnouncing, Is.False);
                AudioSource[] created =
                {
                    (AudioSource)typeof(InterfaceSfxService).GetField("_ui", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(service),
                    (AudioSource)typeof(InterfaceSfxService).GetField("_announcer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(service)
                };
                foreach (AudioSource source in created)
                {
                    Assert.That(source.spatialBlend, Is.Zero);
                    Assert.That(source.ignoreListenerPause, Is.True);
                    Assert.That(source.playOnAwake, Is.False);
                }
                Assert.That(GetEvent(abilities, "TargetRejected").GetInvocationList(), Has.Length.EqualTo(1));
                Assert.That(GetEvent(abilities, "TargetingCanceled").GetInvocationList(), Has.Length.EqualTo(1));
                service.LateDispose();
                Assert.That(root.transform.childCount, Is.Zero);
                Assert.That(GetEvent(abilities, "TargetRejected"), Is.Null);
                Assert.That(GetEvent(abilities, "TargetingCanceled"), Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(data);
                UnityEngine.Object.DestroyImmediate(shipData);
            }
        }

        private static Delegate GetEvent(object instance, string name) => (Delegate)instance.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);

        [Test]
        public void AnnouncementPriority_ProtectsWarningsAndAllowsDefeatToInterrupt()
        {
            InterfaceSfxData data = ScriptableObject.CreateInstance<InterfaceSfxData>();
            ShipSfxData shipData = ScriptableObject.CreateInstance<ShipSfxData>();
            GameObject root = new GameObject("Announcement priority test");
            AudioClip progress = AudioClip.Create("Progress", 441000, 1, 44100, false);
            AudioClip warning = AudioClip.Create("Warning", 441000, 1, 44100, false);
            AudioClip defeat = AudioClip.Create("Defeat", 441000, 1, 44100, false);
            ShipSfxSources sources = root.AddComponent<ShipSfxSources>();
            SetField(sources, "voice", root.AddComponent<AudioSource>());
            InterfaceSfxService service = new InterfaceSfxService(data, shipData, sources,
                new ShipAbilityService(null, TestPlayers.CreateDuel()),
                TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()));
            service.Initialize();
            try
            {
                Assert.That(service.TryPlayAnnouncement(progress, AnnouncementPriority.Progress), Is.True);
                Assert.That(service.IsAnnouncing, Is.True);
                Assert.That(service.TryPlayAnnouncement(warning, AnnouncementPriority.Warning), Is.True);
                Assert.That(service.TryPlayAnnouncement(progress, AnnouncementPriority.Progress), Is.False);
                Assert.That(service.TryPlayAnnouncement(progress, AnnouncementPriority.Notification), Is.False);
                Assert.That(service.TryPlayAnnouncement(warning, AnnouncementPriority.Warning), Is.False);
                AudioSource announcer = (AudioSource)typeof(InterfaceSfxService)
                    .GetField("_announcer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(service);
                service.PlayAnnouncement(progress);
                Assert.That(announcer.clip, Is.SameAs(progress), "Orbital speech replaces a station warning.");
                service.PlayAnnouncement(warning);
                Assert.That(announcer.clip, Is.SameAs(warning), "New orbital speech replaces previous orbital speech.");
                Assert.That(service.TryPlayAnnouncement(defeat, AnnouncementPriority.Defeat), Is.True);
                Assert.That(service.TryPlayAnnouncement(warning, AnnouncementPriority.Warning), Is.False);
                Assert.That(service.TryPlayAnnouncement(defeat, AnnouncementPriority.Defeat), Is.False);
                service.PlayAnnouncement(progress);
                Assert.That(announcer.clip, Is.SameAs(defeat));
            }
            finally
            {
                service.LateDispose();
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(data);
                UnityEngine.Object.DestroyImmediate(shipData);
                UnityEngine.Object.DestroyImmediate(progress);
                UnityEngine.Object.DestroyImmediate(warning);
                UnityEngine.Object.DestroyImmediate(defeat);
            }
        }

        [TestCase(AnnouncementPriority.Progress, false)]
        [TestCase(AnnouncementPriority.Notification, false)]
        [TestCase(AnnouncementPriority.Warning, false)]
        [TestCase(AnnouncementPriority.Warning, true)]
        [TestCase(AnnouncementPriority.Defeat, false)]
        public void StationUpgradeFollowUp_ReservesItsTurnAndAllowsUrgentInterruption(AnnouncementPriority priority, bool orbital)
        {
            InterfaceSfxData data = ScriptableObject.CreateInstance<InterfaceSfxData>();
            ShipSfxData shipData = ScriptableObject.CreateInstance<ShipSfxData>();
            GameObject root = new GameObject("Announcement sequence test");
            AudioClip upgrade = AudioClip.Create("Upgrade", 441000, 1, 44100, false);
            AudioClip construction = AudioClip.Create("Construction", 441000, 1, 44100, false);
            AudioClip interrupt = AudioClip.Create("Interrupt", 441000, 1, 44100, false);
            ShipSfxSources sources = root.AddComponent<ShipSfxSources>();
            SetField(sources, "voice", root.AddComponent<AudioSource>());
            InterfaceSfxService service = new InterfaceSfxService(data, shipData, sources,
                new ShipAbilityService(null, TestPlayers.CreateDuel()),
                TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()));
            service.Initialize();
            try
            {
                AudioSource announcer = (AudioSource)typeof(InterfaceSfxService)
                    .GetField("_announcer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(service);
                Assert.That(service.TryPlayStationUpgrade(upgrade, construction), Is.True);
                service.Tick();
                Assert.That(announcer.clip, Is.SameAs(upgrade), "Follow-up waits for playback to finish.");
                announcer.Stop();
                Assert.That(service.IsAnnouncing, Is.True, "The pending follow-up reserves the inter-line gap.");
                bool urgent = priority < AnnouncementPriority.Notification;
                if (orbital) service.PlayAnnouncement(interrupt);
                else Assert.That(service.TryPlayAnnouncement(interrupt, priority), Is.EqualTo(urgent));
                service.Tick();
                Assert.That(announcer.clip, Is.SameAs(urgent ? interrupt : construction));
                announcer.Stop();
                service.Tick();
                Assert.That(service.IsAnnouncing, Is.False, "Follow-up plays once or is cancelled by urgent speech.");
            }
            finally
            {
                service.LateDispose();
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(data);
                UnityEngine.Object.DestroyImmediate(shipData);
                UnityEngine.Object.DestroyImmediate(upgrade);
                UnityEngine.Object.DestroyImmediate(construction);
                UnityEngine.Object.DestroyImmediate(interrupt);
            }
        }

        [TestCase(SuperWeaponType.IonCannon)]
        [TestCase(SuperWeaponType.HypervelocityGun)]
        [TestCase(SuperWeaponType.PlasmaCannon)]
        public void Announcer_SuppressesStartupAndRestoreButAnnouncesPurchaseAndFireableReady(SuperWeaponType type)
        {
            InterfaceSfxData data = ScriptableObject.CreateInstance<InterfaceSfxData>();
            AudioClip bought = AudioClip.Create("Bought", 1, 1, 44100, false);
            AudioClip ready = AudioClip.Create("Ready", 1, 1, 44100, false);
            AudioClip firing = AudioClip.Create("Firing", 1, 1, 44100, false);
            try
            {
                string chargingField = type switch
                {
                    SuperWeaponType.IonCannon => "ionCharging",
                    SuperWeaponType.HypervelocityGun => "hypervelocityCharging",
                    _ => "orbitalBought"
                };
                string availableField = type switch
                {
                    SuperWeaponType.IonCannon => "ionAvailable",
                    SuperWeaponType.HypervelocityGun => "hypervelocityAvailable",
                    _ => "orbitalAvailable"
                };
                string firingField = type switch
                {
                    SuperWeaponType.IonCannon => "ionFiring",
                    SuperWeaponType.HypervelocityGun => "hypervelocityFiring",
                    _ => "orbitalFiring"
                };
                SetField(data, chargingField, bought);
                SetField(data, availableField, ready);
                SetField(data, firingField, firing);
                SuperWeaponModel model = new SuperWeaponModel();
                RecordingInterfaceAudio audio = new RecordingInterfaceAudio();
                FireService fire = new FireService();
                SuperWeaponPresenter presenter = new SuperWeaponPresenter(
                    superWeaponFireService: fire, viewProvider: null, routeNavigation: null, abilities: null,
                    localPlayer: TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()), uiService: null, cancelRouter: null, tooltipService: null,
                    faction: null, bindings: null, battleState: null, model: model,
                    targeting: new SuperWeaponTargetingModel(), unitTargeting: null, data: null,
                    interfaceAudio: audio, interfaceData: data);
                SetField(presenter, "_view", new View());
                var states = (Dictionary<SuperWeaponType, SuperWeaponState>)typeof(SuperWeaponPresenter)
                    .GetField("_lastStates", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(presenter);
                // The shared cooldown re-announces every weapon type when it ends.
                foreach (SuperWeaponType weapon in Enum.GetValues(typeof(SuperWeaponType)))
                    states[weapon] = SuperWeaponState.Unavailable;
                MethodInfo handler = typeof(SuperWeaponPresenter).GetMethod("HandleStateChanged",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                model.OnStateChanged += (weapon, state) => handler.Invoke(presenter, new object[] { weapon, state });

                model.StartCharging(type);
                model.CompleteCharging(type);
                Assert.That(audio.Clips, Is.Empty, "Battle initialization must be silent.");
                presenter.UpdateState(BattleState.Running);
                model.Restore(type, SuperWeaponState.Ready, 0f);
                Assert.That(audio.Clips, Is.Empty, "Save loading must be silent.");
                Assert.That(model.IsRestoring, Is.False);
                model.Consume(type);
                model.StartCharging(type);
                Assert.That(audio.Clips, Is.EqualTo(new[] { bought }));
                model.CompleteCharging(type);
                Assert.That(audio.Clips, Is.EqualTo(new[] { bought }), "A cooldown-blocked charge is not available.");
                model.Tick(SuperWeaponModel.COOLDOWN_DURATION);
                Assert.That(audio.Clips, Is.EqualTo(new[] { bought, ready }));
                MethodInfo submit = typeof(SuperWeaponPresenter).GetMethod("HandleTargetSubmitted",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                submit.Invoke(presenter, new object[] { type, null });
                Assert.That(audio.Rejections, Is.EqualTo(1));
                Assert.That(model.CanFire(type), Is.True, "Rejected targets must retain the charge.");
                Assert.That(fire.Shots, Is.Zero);
                fire.Accept = true;
                submit.Invoke(presenter, new object[] { type, null });
                Assert.That(audio.Clips, Is.EqualTo(new[] { bought, ready, firing }));
                Assert.That(fire.Shots, Is.EqualTo(1));
                Assert.That(model.CanFire(type), Is.False);

                MethodInfo fired = typeof(SuperWeaponPresenter).GetMethod("HandleFired",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                fired.Invoke(presenter, new object[] { type, new OwnedEntity(TestPlayers.Enemy) });
                Assert.That(audio.Clips, Has.Count.EqualTo(3), "Own strikes are announced on submit only.");
                fired.Invoke(presenter, new object[] { type, new OwnedEntity(TestPlayers.Human) });
                Assert.That(audio.Clips, Is.EqualTo(new[] { bought, ready, firing, firing }),
                    "An incoming enemy strike warns the local player.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(data);
                UnityEngine.Object.DestroyImmediate(bought);
                UnityEngine.Object.DestroyImmediate(ready);
                UnityEngine.Object.DestroyImmediate(firing);
            }
        }

        [TestCase(SuperWeaponType.IonCannon, "I000_RHD6301_ENG", "I000_RHD2401_ENG", "I000_RHD6302_ENG")]
        [TestCase(SuperWeaponType.HypervelocityGun, "I000_EHD6303_ENG", "I000_EHD2402_ENG", "I000_EHD6304_ENG")]
        public void WeaponAnnouncementsUseNamedChargingReadyAndFiringRecordings(
            SuperWeaponType type, string charging, string ready, string firing)
        {
            var data = AssetDatabase.LoadAssetAtPath<InterfaceSfxData>(
                "Assets/Settings/Data/Models/Audio/InterfaceSfxData.asset");
            Assert.That(data.GetOrbitalBought(type).name, Is.EqualTo(charging));
            Assert.That(data.GetOrbitalAvailable(type).name, Is.EqualTo(ready));
            Assert.That(data.GetOrbitalFiring(type).name, Is.EqualTo(firing));
        }

        [Test]
        public void PlasmaRetainsGenericOrbitalAnnouncements()
        {
            var data = AssetDatabase.LoadAssetAtPath<InterfaceSfxData>(
                "Assets/Settings/Data/Models/Audio/InterfaceSfxData.asset");
            using var serialized = new SerializedObject(data);
            Assert.That(data.GetOrbitalBought(SuperWeaponType.PlasmaCannon),
                Is.SameAs(serialized.FindProperty("orbitalBought").objectReferenceValue));
            Assert.That(data.GetOrbitalAvailable(SuperWeaponType.PlasmaCannon),
                Is.SameAs(serialized.FindProperty("orbitalAvailable").objectReferenceValue));
            Assert.That(data.GetOrbitalFiring(SuperWeaponType.PlasmaCannon),
                Is.SameAs(serialized.FindProperty("orbitalFiring").objectReferenceValue));
        }

        private static void SetField(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

        private sealed class RecordingInterfaceAudio : IInterfaceSfxService
        {
            public List<AudioClip> Clips { get; } = new();
            public bool IsAnnouncing => false;
            public int Rejections { get; private set; }
            public void PlayRejected() => Rejections++;
            public void PlayAbilityTargetingCanceled() { }
            public void PlayAnnouncement(AudioClip clip) => Clips.Add(clip);
            public bool TryPlayAnnouncement(AudioClip clip, AnnouncementPriority priority) { Clips.Add(clip); return true; }
            public bool TryPlayStationUpgrade(AudioClip clip, AudioClip newConstruction) { Clips.Add(clip); return true; }
        }

        private sealed class FireService : ISuperWeaponFireService
        {
            public bool Accept { get; set; }
            public int Shots { get; private set; }
            public event Action<SuperWeaponType, IEntity> Fired { add { } remove { } }
            public bool CanTarget(PlayerId owner, IEntity target) => Accept;
            public void Fire(SuperWeaponType type, IEntity target) => Shots++;
        }

        private sealed class OwnedEntity : IEntity
        {
            public long Id => 1;
            public EmpireAtWar.Models.Health.IHealthModelObserver HealthModel => throw new NotSupportedException();
            public PlayerId Owner { get; }
            public OwnedEntity(PlayerId owner) => Owner = owner;
            public bool TryGetFacade<TFacade>(out TFacade entityFacade) where TFacade : IEntityFacade =>
                throw new NotSupportedException();
            public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade => throw new NotSupportedException();
        }

        private sealed class View : ISuperWeaponsView
        {
            public TooltipHoverView TooltipHover => null;
            public event Action<SuperWeaponType> Pressed { add { } remove { } }
            public event Action ToggleRequested { add { } remove { } }
            public event Action CloseRequested { add { } remove { } }
            public void Initialize() { }
            public void Dispose() { }
            public void SetState(SuperWeaponType type, SuperWeaponState state, bool canFire) { }
            public void SetPending(SuperWeaponType? type) { }
            public void SetOpen(bool open) { }
            public void SetAvailable(bool available) { }
            public void SetBattleAvailable(bool available) { }
            public void SetRemaining(SuperWeaponType type, float seconds) { }
        }
    }
}
