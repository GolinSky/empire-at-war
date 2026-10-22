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
                SetField(data, "orbitalBought", bought);
                SetField(data, "orbitalAvailable", ready);
                SetField(data, "orbitalFiring", firing);
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
                states[type] = SuperWeaponState.Unavailable;
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
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(data);
                UnityEngine.Object.DestroyImmediate(bought);
                UnityEngine.Object.DestroyImmediate(ready);
                UnityEngine.Object.DestroyImmediate(firing);
            }
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
        }

        private sealed class FireService : ISuperWeaponFireService
        {
            public bool Accept { get; set; }
            public int Shots { get; private set; }
            public bool CanTarget(PlayerId owner, IEntity target) => Accept;
            public void Fire(SuperWeaponType type, IEntity target) => Shots++;
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
