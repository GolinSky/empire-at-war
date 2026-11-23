using EmpireAtWar.Entities.CoreGameUi;
using System;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.Faction.Ui;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.SuperWeapons.Controller;
using EmpireAtWar.Entities.SuperWeapons.Ui;
using EmpireAtWar.Entities.UnitActions.Model;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.Input;
using PlayerRegistry = EmpireAtWar.Services.Player.PlayerRegistry;
using EmpireAtWar.Services.PlayerCommands;
using EmpireAtWar.Services.Popups;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.Stations;
using EmpireAtWar.Services.SuperWeapons;
using EmpireAtWar.Services.UnitExplosion;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Mvc;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.ViewComponents.Weapon;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Abilities")]
    public sealed class SuperWeaponTests
    {
        [TestCase(SuperWeaponType.IonCannon)]
        [TestCase(SuperWeaponType.HypervelocityGun)]
        [TestCase(SuperWeaponType.PlasmaCannon)]
        public void Enemy_OnlyFiresAtStationWhileTeamCanSeeIt(SuperWeaponType type)
        {
            GameObject stationView = new GameObject("Station target");
            GameObject scout = new GameObject("Enemy scout");
            try
            {
                stationView.transform.position = Vector3.right * 100f;
                PlayerRoster roster = TestPlayers.CreateTeamGame();
                VisionService vision = new VisionService(roster);
                EntityLocator entities = new EntityLocator();
                StationTarget health = new StationTarget(stationView.transform);
                Entity station = new Entity(health, entities, new IEntityFacade[] { health },
                    TestPlayers.Human, 1, System.Array.Empty<IPooledEntityEvents>(),
                    System.Array.Empty<IHangarLaunchEvents>());
                entities.AddEntity(station);
                RecordingSuperWeaponFireService fireService = new RecordingSuperWeaponFireService();
                SuperWeaponModel model = new SuperWeaponModel();
                // Charging, purchasing and economy are outside this firing regression.
                EnemySuperWeaponController controller = new EnemySuperWeaponController(
                    fireService, entities, vision, null, model, null, null, roster.Get(TestPlayers.Enemy), null,
                    LoadData(), roster, new EnemyStationPressureTracker());
                MethodInfo tryFire = typeof(EnemySuperWeaponController).GetMethod("TryFire",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                model.StartCharging(type);
                model.CompleteCharging(type);

                // The player's own vision must not reveal the station to the enemy.
                vision.Register(TestPlayers.Human, stationView.transform, 20f);
                tryFire.Invoke(controller, new object[] { type });
                Assert.That(fireService.Target, Is.Null, "A station in enemy fog cannot be targeted.");
                Assert.That(model.CanFire(type), Is.True, "Fog must not consume the charged weapon.");

                scout.transform.position = stationView.transform.position;
                vision.Register(TestPlayers.Enemy, scout.transform, 20f);
                // Previously seen stations must become untargetable when vision is lost.
                vision.Unregister(scout.transform);
                tryFire.Invoke(controller, new object[] { type });
                Assert.That(fireService.Target, Is.Null);
                Assert.That(model.CanFire(type), Is.True);

                vision.Register(TestPlayers.Enemy, scout.transform, 20f);
                tryFire.Invoke(controller, new object[] { type });
                Assert.That(fireService.Target, Is.SameAs(station));
                Assert.That(model.CanFire(type), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(stationView);
                UnityEngine.Object.DestroyImmediate(scout);
            }
        }

        private sealed class RecordingSuperWeaponFireService : ISuperWeaponFireService
        {
            public IEntity Target { get; private set; }
            public event Action<PlayerId, SuperWeaponType, IEntity> Fired { add { } remove { } }
            public bool CanTarget(PlayerId owner, IEntity target) => target.Owner != owner;
            public void Fire(PlayerId issuer, SuperWeaponType type, IEntity target) => Target = target;
        }

        [Test]
        public void Enemy_WaitsForItsDifficultyDelayBeforeFiring()
        {
            GameObject stationView = new GameObject("Station target");
            GameObject scout = new GameObject("Enemy scout");
            try
            {
                PlayerRoster roster = TestPlayers.CreateTeamGame();
                VisionService vision = new VisionService(roster);
                EntityLocator entities = new EntityLocator();
                StationTarget health = new StationTarget(stationView.transform);
                entities.AddEntity(new Entity(health, entities, new IEntityFacade[] { health },
                    TestPlayers.Human, 1, Array.Empty<IPooledEntityEvents>(),
                    Array.Empty<IHangarLaunchEvents>()));
                vision.Register(TestPlayers.Enemy, scout.transform, 20f);
                RecordingSuperWeaponFireService fireService = new RecordingSuperWeaponFireService();
                SuperWeaponModel model = new SuperWeaponModel();
                EnemySuperWeaponController controller = new EnemySuperWeaponController(
                    fireService, entities, vision, null, model, null, null, roster.Get(TestPlayers.Enemy), null,
                    LoadData(), roster, new EnemyStationPressureTracker());
                MethodInfo tryFire = typeof(EnemySuperWeaponController).GetMethod("TryFireAfterDelay",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo delay = typeof(EnemySuperWeaponController).GetField("_fireDelayLeft",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                model.StartCharging(SuperWeaponType.PlasmaCannon);
                model.CompleteCharging(SuperWeaponType.PlasmaCannon);

                tryFire.Invoke(controller, new object[] { SuperWeaponType.PlasmaCannon });
                Assert.That(fireService.Target, Is.Null, "A newly fireable weapon starts the reaction delay.");
                Assert.That((float)delay.GetValue(controller), Is.GreaterThan(0f));
                tryFire.Invoke(controller, new object[] { SuperWeaponType.PlasmaCannon });
                Assert.That(fireService.Target, Is.Null, "The weapon holds until the delay has passed.");

                delay.SetValue(controller, 0f);
                tryFire.Invoke(controller, new object[] { SuperWeaponType.PlasmaCannon });
                Assert.That(fireService.Target, Is.Not.Null);
                Assert.That(model.CanFire(SuperWeaponType.PlasmaCannon), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(stationView);
                UnityEngine.Object.DestroyImmediate(scout);
            }
        }

        [TestCase(SuperWeaponType.IonCannon)]
        [TestCase(SuperWeaponType.HypervelocityGun)]
        [TestCase(SuperWeaponType.PlasmaCannon)]
        public void ResearchDamage_CoversEveryOrbitalResearchTierAndGrows(SuperWeaponType type)
        {
            SuperWeaponProfile profile = LoadData().GetProfile(type);
            int checkedLines = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:FactionDefinition"))
            {
                FactionDefinition faction = AssetDatabase.LoadAssetAtPath<FactionDefinition>(
                    AssetDatabase.GUIDToAssetPath(guid));
                Assert.That(faction.ResearchLines.TryGetValue(ResearchType.OrbitalWeapons, out ResearchLineData line),
                    Is.True, $"{faction.name} has no Orbital Weapons research.");
                checkedLines++;
                for (int tier = 1; tier <= line.Tiers.Length; tier++)
                {
                    Assert.That(profile.GetDamage(tier), Is.GreaterThan(profile.GetDamage(tier - 1)),
                        $"{faction.name} {type} tier {tier}");
                    if (profile.AreaDamage > 0f)
                        Assert.That(profile.GetAreaDamage(tier), Is.GreaterThan(profile.GetAreaDamage(tier - 1)),
                            $"{faction.name} {type} area tier {tier}");
                }
            }

            Assert.That(checkedLines, Is.GreaterThan(0));
        }

        [Test]
        public void Fire_DealsReducedDamageToSuperWeaponStunnedTargets()
        {
            GameObject view = new GameObject("Stunned target");
            GameObject audio = new GameObject("Test audio");
            EntityLocator entities = new EntityLocator();
            StationTarget health = new StationTarget(view.transform);
            Entity target = new Entity(health, entities, new IEntityFacade[] { health },
                TestPlayers.Human, 123, Array.Empty<IPooledEntityEvents>(),
                Array.Empty<IHangarLaunchEvents>());
            entities.AddEntity(target);
            SuperWeaponData data = LoadData();
            SuperWeaponFireService fire = CreateFireService(entities, data, audio);
            try
            {
                SuperWeaponProfile profile = data.GetProfile(SuperWeaponType.HypervelocityGun);
                MethodInfo applyDamage = typeof(SuperWeaponFireService).GetMethod("ApplyDamage",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                applyDamage.Invoke(fire, new object[] { target, 300f, profile });
                Assert.That(health.LastDamage, Is.EqualTo(300f));

                fire.RestoreStun(new SuperWeaponStun(target, health.Modifiers,
                    new CombatStatModifier(1f, 1f, 1f, 1f, 1f), 5f));
                applyDamage.Invoke(fire, new object[] { target, 300f, profile });
                Assert.That(profile.StunnedTargetDamageMultiplier, Is.LessThan(1f));
                Assert.That(health.LastDamage, Is.EqualTo(300f * profile.StunnedTargetDamageMultiplier));
            }
            finally
            {
                fire.LateDispose();
                UnityEngine.Object.DestroyImmediate(view);
                UnityEngine.Object.DestroyImmediate(audio);
            }
        }

        private static SuperWeaponData LoadData() => AssetDatabase.LoadAssetAtPath<SuperWeaponData>(
            "Assets/Settings/Data/Models/SuperWeapons/SuperWeaponData.asset");

        private sealed class StationTarget : IHealthModelObserver, IEntityTransformFacade, IHealthFacade, ICombatModifiersFacade,
            IUnitExplosionFacade
        {
            public CombatModifiers Modifiers { get; } = new CombatModifiers();
            public int HitCount { get; private set; }
            public float LastDamage { get; private set; }
            public int ExplosionCount { get; private set; }
            public UnitExplosionKind LastExplosionKind { get; private set; }
            public void PlayExplosion(UnitExplosionKind kind)
            {
                ExplosionCount++;
                LastExplosionKind = kind;
            }
            public bool Destroyed { get; set; }
            public void ApplyDamage(float damage, DamageType damageType, int id)
            {
                HitCount++;
                LastDamage = damage;
            }
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
            public Transform Transform { get; }
            public ShipClass ShipClass => ShipClass.Structure;
            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1000f;
            public float HullPercentage => 1f;
            public float Shields => 1000f;
            public float ShieldPercentage => 1f;
            public bool IsDestroyed => Destroyed;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public bool HasLiveHardPoints => true;
            public bool HasShields => true;
            public PlayerId Owner => TestPlayers.Human;
            public StationTarget(Transform transform) => Transform = transform;
            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) =>
                Array.Empty<IHardPointModel>();
        }

        [TestCase(SuperWeaponType.IonCannon)]
        [TestCase(SuperWeaponType.HypervelocityGun)]
        [TestCase(SuperWeaponType.PlasmaCannon)]
        public void Save_RestoresPendingSuperWeaponImpactsWithoutRepeatingShots(SuperWeaponType type)
        {
            GameObject view = new GameObject("Saved target");
            GameObject origin = new GameObject("Saved origin");
            GameObject audio = new GameObject("Test audio");
            EntityLocator entities = new EntityLocator();
            StationTarget health = new StationTarget(view.transform);
            Entity target = new Entity(health, entities, new IEntityFacade[] { health },
                TestPlayers.Human, 123, Array.Empty<IPooledEntityEvents>(),
                Array.Empty<IHangarLaunchEvents>());
            entities.AddEntity(target);
            SuperWeaponData data = AssetDatabase.LoadAssetAtPath<SuperWeaponData>(
                "Assets/Settings/Data/Models/SuperWeapons/SuperWeaponData.asset");
            SuperWeaponFireService fire = CreateFireService(entities, data, audio);
            RecordingAudio restoredAudio = new RecordingAudio();
            SuperWeaponFireService restored = CreateFireService(entities, data, audio, restoredAudio);
            try
            {
                origin.transform.position = new Vector3(3f, 5f, 7f);
                view.transform.position = new Vector3(10f, 20f, 30f);
                SuperWeaponSalvo salvo = new SuperWeaponSalvo(type, target, data.GetProfile(type), origin.transform)
                {
                    ShotsFired = data.GetProfile(type).Weapon.ShotsPerSalvo,
                    NextShotTime = 0.75f
                };
                salvo.ImpactTimes.AddRange(new[] { 0.1f, 0.3f });
                fire.RestoreSalvo(salvo);
                fire.RestoreSalvo(new SuperWeaponSalvo(type, target, data.GetProfile(type), origin.transform)
                {
                    ShotsFired = 0,
                    NextShotTime = 1.25f
                });
                BattleSave save = new BattleSave();
                new SuperWeaponCombatSaveSource(fire, entities, data).Capture(save);
                new SuperWeaponCombatSaveSource(restored, entities, data).RestoreAfterUnits(
                    new BattleRestoreContext(BattleSave.FromJson(save.ToJson())));

                SuperWeaponSalvo loaded = restored.Salvos[0];
                Assert.That(loaded.Type, Is.EqualTo(type));
                Assert.That(loaded.Target, Is.SameAs(target));
                Assert.That(loaded.Origin.position, Is.EqualTo(origin.transform.position));
                Assert.That(loaded.ShotsFired, Is.EqualTo(salvo.ShotsFired));
                Assert.That(loaded.NextShotTime, Is.EqualTo(0.75f));
                Assert.That(restored.Salvos[1].ShotsFired, Is.Zero, "A warming-up salvo must stay unfired.");
                Assert.That(restored.Salvos[1].NextShotTime, Is.EqualTo(1.25f));
                Assert.That(restored.Salvos[1].ImpactTimes, Is.Empty);
                MethodInfo advance = typeof(SuperWeaponFireService).GetMethod("AdvanceSalvo",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                bool explosive = type != SuperWeaponType.IonCannon;
                Assert.That(health.ExplosionCount, Is.Zero);
                advance.Invoke(restored, new object[] { loaded, 0.11f });
                Assert.That(health.HitCount, Is.EqualTo(1));
                Assert.That(health.ExplosionCount, Is.Zero, "Only the salvo's last impact explodes.");
                Assert.That(loaded.IsComplete, Is.False);
                advance.Invoke(restored, new object[] { loaded, 0.2f });
                Assert.That(health.HitCount, Is.EqualTo(2));
                Assert.That(loaded.IsComplete, Is.True);
                Assert.That(health.ExplosionCount, Is.EqualTo(explosive ? 1 : 0),
                    "An explosive salvo plays one hull-sized explosion on its last impact; the ion cannon never does.");
                if (explosive) Assert.That(health.LastExplosionKind, Is.EqualTo(UnitExplosionKind.OrbitalHit));
                Assert.That(restoredAudio.Clips, Is.Empty, "Restoring and resolving already-fired impacts must not replay launch SFX.");
                advance.Invoke(restored, new object[] { loaded, 1f });
                Assert.That(health.HitCount, Is.EqualTo(2), "Loaded impacts must apply exactly once.");
                Assert.That(health.ExplosionCount, Is.EqualTo(explosive ? 1 : 0));
                health.Destroyed = true;
                new SuperWeaponCombatSaveSource(fire, entities, data).Capture(save);
                Assert.That(save.SuperWeaponSalvos, Is.Empty, "Dead targets are absent from the saved entities.");
            }
            finally
            {
                foreach (SuperWeaponSalvo salvo in restored.Salvos)
                    UnityEngine.Object.DestroyImmediate(salvo.Origin.gameObject);
                UnityEngine.Object.DestroyImmediate(origin);
                fire.LateDispose();
                restored.LateDispose();
                UnityEngine.Object.DestroyImmediate(view);
                UnityEngine.Object.DestroyImmediate(audio);
            }
        }

        [Test]
        public void Save_RestoresSuperWeaponStunAndRemovesItWhenExpired()
        {
            GameObject view = new GameObject("Stunned target");
            GameObject audio = new GameObject("Test audio");
            EntityLocator entities = new EntityLocator();
            StationTarget health = new StationTarget(view.transform);
            Entity target = new Entity(health, entities, new IEntityFacade[] { health },
                TestPlayers.Human, 123, Array.Empty<IPooledEntityEvents>(),
                Array.Empty<IHangarLaunchEvents>());
            entities.AddEntity(target);
            SuperWeaponFireService fire = CreateFireService(entities, null, audio);
            SuperWeaponFireService restored = CreateFireService(entities, null, audio);
            try
            {
                CombatStatModifier modifier = new CombatStatModifier(0.5f, 1f, 0.25f, 1f, 1f);
                fire.RestoreStun(new SuperWeaponStun(target, health.Modifiers, modifier, 3.5f));
                BattleSave save = new BattleSave();
                SuperWeaponCombatSaveSource source = new SuperWeaponCombatSaveSource(fire, entities, null);
                source.Capture(save);
                fire.LateDispose();
                new SuperWeaponCombatSaveSource(restored, entities, null).RestoreAfterUnits(
                    new BattleRestoreContext(BattleSave.FromJson(save.ToJson())));
                Assert.That(health.Modifiers.IsIonDisabled, Is.True);
                Assert.That(health.Modifiers.SpeedMultiplier, Is.EqualTo(0.25f));
                Assert.That(restored.Stuns[0].TimeLeft, Is.EqualTo(3.5f));
                restored.Stuns[0].TimeLeft = 0f;
                restored.Tick();
                Assert.That(restored.Stuns, Is.Empty);
                Assert.That(health.Modifiers.IsIonDisabled, Is.False);
                Assert.That(health.Modifiers.SpeedMultiplier, Is.EqualTo(1f));
            }
            finally
            {
                fire.LateDispose();
                restored.LateDispose();
                UnityEngine.Object.DestroyImmediate(view);
                UnityEngine.Object.DestroyImmediate(audio);
            }
        }

        [Test]
        public void Save_OlderSaveHasNoSuperWeaponCombatEffects()
        {
            BattleSave save = BattleSave.FromJson("{}");
            // A pre-feature save must not try to restore any runtime effects.
            new SuperWeaponCombatSaveSource(null, null, null).RestoreAfterUnits(new BattleRestoreContext(save));
        }

        private static SuperWeaponFireService CreateFireService(EntityLocator entities, SuperWeaponData data, GameObject audio,
            IAudioService audioService = null, BoltFlightBatch boltFlights = null)
        {
            AudioSource audioSource = audio.AddComponent<AudioSource>();
            ShipSfxSources sources = audio.AddComponent<ShipSfxSources>();
            typeof(ShipSfxSources).GetField("sfx", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(sources, new[] { audioSource });
            ShipSfxData sfx = AssetDatabase.LoadAssetAtPath<ShipSfxData>(
                "Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
            return new SuperWeaponFireService(entities, null, TestPlayers.CreateTeamGame(), new PlayerRegistry(), data,
                new ImpactEffectPresenter(null, null), boltFlights, audioService, sfx, sources);
        }


        private sealed class RecordingAudio : Service, IAudioService
        {
            public readonly List<AudioClip> Clips = new List<AudioClip>();
            public readonly List<float> Volumes = new List<float>();
            public void PlayOneShot(AudioSource source, AudioClip clip, float volume)
            {
                Clips.Add(clip);
                Volumes.Add(volume);
            }
            public void Play(AudioSource source, AudioClip clip, float volume, bool loop) { }
            public void Stop(AudioSource source) { }
            public void Pause(AudioSource source) { }
            public void UnPause(AudioSource source) { }
            public void SetGamePaused(bool paused) { }
        }

        [TestCase(SuperWeaponType.IonCannon, "GUN_Ion_Cannon_1")]
        [TestCase(SuperWeaponType.HypervelocityGun, "GUN_HYPERVELOCITY_1")]
        [TestCase(SuperWeaponType.PlasmaCannon, "GUN_UNDERWORLD_PLASMA_1")]
        public void ShotPlaysWeaponSpecificRecordingAtConfiguredGain(SuperWeaponType type, string clipName)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var existingRoots = new HashSet<GameObject>(scene.GetRootGameObjects());
            GameObject view = new GameObject("Ion target");
            GameObject origin = new GameObject("Ion origin");
            GameObject audio = new GameObject("Ion test audio");
            var recording = new RecordingAudio();
            var boltFlights = new BoltFlightBatch();
            var entities = new EntityLocator();
            var health = new StationTarget(view.transform);
            var target = new Entity(health, entities, new IEntityFacade[] { health }, TestPlayers.Human, 123,
                Array.Empty<IPooledEntityEvents>(), Array.Empty<IHangarLaunchEvents>());
            SuperWeaponData data = LoadData();
            SuperWeaponFireService fire = CreateFireService(entities, data, audio, recording, boltFlights);
            try
            {
                view.transform.position = Vector3.forward * 100f;
                SuperWeaponProfile profile = data.GetProfile(type);
                var salvo = new SuperWeaponSalvo(type, target, profile, origin.transform);
                MethodInfo advance = typeof(SuperWeaponFireService).GetMethod("AdvanceSalvo",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                salvo.NextShotTime = 1f;
                advance.Invoke(fire, new object[] { salvo, 0.5f });
                Assert.That(recording.Clips, Is.Empty, "Warmup must not play the firing sound.");
                advance.Invoke(fire, new object[] { salvo, 0.5f });
                Assert.That(salvo.ShotsFired, Is.EqualTo(1));
                Assert.That(recording.Clips, Has.Count.EqualTo(1));
                Assert.That(recording.Clips[0].name, Is.EqualTo(clipName));
                Assert.That(recording.Volumes, Is.EqualTo(new[] { 0.8f }));
            }
            finally
            {
                fire.LateDispose();
                boltFlights.Dispose();
                foreach (GameObject root in scene.GetRootGameObjects())
                    if (!existingRoots.Contains(root)) UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Model_OnePurchaseIsOneShot()
        {
            SuperWeaponModel model = new SuperWeaponModel();
            List<SuperWeaponState> states = new List<SuperWeaponState>();
            model.OnStateChanged += (_, state) => states.Add(state);

            model.StartCharging(SuperWeaponType.IonCannon);
            Assert.That(model.CanPurchase(SuperWeaponType.IonCannon), Is.False);
            model.CompleteCharging(SuperWeaponType.IonCannon);
            Assert.That(model.CanPurchase(SuperWeaponType.IonCannon), Is.False);
            model.Consume(SuperWeaponType.IonCannon);

            Assert.That(model.CanPurchase(SuperWeaponType.IonCannon), Is.True);
            Assert.That(states, Is.EqualTo(new[]
            {
                SuperWeaponState.Charging, SuperWeaponState.Ready, SuperWeaponState.Unavailable
            }));
        }

        [Test]
        public void Model_CooldownBlocksFiringButNotRebuilding()
        {
            SuperWeaponModel model = new SuperWeaponModel();
            model.StartCharging(SuperWeaponType.IonCannon);
            model.CompleteCharging(SuperWeaponType.IonCannon);
            model.Consume(SuperWeaponType.IonCannon);

            model.StartCharging(SuperWeaponType.IonCannon);
            model.CompleteCharging(SuperWeaponType.IonCannon);
            model.Tick(SuperWeaponModel.COOLDOWN_DURATION - 1f);
            Assert.That(model.CanFire(SuperWeaponType.IonCannon), Is.False);
            Assert.Throws<InvalidOperationException>(() => model.Consume(SuperWeaponType.IonCannon));

            model.Tick(1f);
            Assert.That(model.CanFire(SuperWeaponType.IonCannon), Is.True);
        }

        [Test]
        public void Model_AllThreeWeaponsCanFireWithoutWaitingForEachOther()
        {
            SuperWeaponModel model = new SuperWeaponModel();
            SuperWeaponType[] weapons = (SuperWeaponType[])Enum.GetValues(typeof(SuperWeaponType));
            foreach (SuperWeaponType type in weapons)
            {
                model.StartCharging(type);
                model.CompleteCharging(type);
            }

            foreach (SuperWeaponType type in weapons)
            {
                Assert.That(model.CanFire(type), Is.True, $"{type} must remain ready when another weapon fires.");
                Assert.That(model.GetCooldownRemaining(type), Is.Zero);
                model.Consume(type);
            }

            foreach (SuperWeaponType type in weapons)
            {
                Assert.That(model.GetState(type), Is.EqualTo(SuperWeaponState.Unavailable));
                Assert.That(model.GetCooldownRemaining(type), Is.EqualTo(SuperWeaponModel.COOLDOWN_DURATION));
                model.StartCharging(type);
                model.CompleteCharging(type);
                Assert.That(model.CanFire(type), Is.False, $"{type} must keep its own cooldown.");
            }

            model.Tick(SuperWeaponModel.COOLDOWN_DURATION);
            foreach (SuperWeaponType type in weapons)
                Assert.That(model.CanFire(type), Is.True);
        }

        [Test]
        public void Model_RejectsFiringAWeaponThatIsNotReady()
        {
            SuperWeaponModel model = new SuperWeaponModel();
            model.StartCharging(SuperWeaponType.PlasmaCannon);

            Assert.Throws<InvalidOperationException>(() => model.Consume(SuperWeaponType.PlasmaCannon));
            Assert.Throws<InvalidOperationException>(() => model.StartCharging(SuperWeaponType.PlasmaCannon));
        }

        [Test]
        public void CoreGamePrefab_WiresPurchaseAndRowButtonsForEverySuperWeapon()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Ui/SkirmishGame/CoreGameUi.prefab");
            SuperWeaponsView view = (SuperWeaponsView)prefab.GetComponent<CoreGameUi>().SuperWeaponsView;
            Assert.That(view, Is.Not.Null);
            SerializedProperty buttons = new SerializedObject(view).FindProperty("buttons");
            Assert.That(buttons.arraySize, Is.EqualTo(Enum.GetValues(typeof(SuperWeaponType)).Length));
            HashSet<SuperWeaponType> seen = new HashSet<SuperWeaponType>();
            for (int i = 0; i < buttons.arraySize; i++)
            {
                SuperWeaponButton weapon =
                    (SuperWeaponButton)buttons.GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.That(weapon, Is.Not.Null);
                Assert.That(seen.Add(weapon.WeaponType), Is.True, $"Duplicate weapon: {weapon.WeaponType}");
                SerializedObject buttonData = new SerializedObject(weapon);
                Button button = (Button)buttonData.FindProperty("button").objectReferenceValue;
                Assert.That(button, Is.Not.Null);
                Assert.That(button.transition, Is.EqualTo(Selectable.Transition.ColorTint));
                Button rowButton = (Button)buttonData.FindProperty("rowButton").objectReferenceValue;
                Assert.That(rowButton, Is.Not.Null);
                Assert.That(rowButton.gameObject, Is.SameAs(weapon.gameObject));
                Assert.That(rowButton.targetGraphic.raycastTarget, Is.True);
                Assert.That(rowButton.transition, Is.EqualTo(Selectable.Transition.ColorTint));
                Assert.That(button.transform.parent, Is.SameAs(rowButton.transform));
                Assert.That(buttonData.FindProperty("pendingHighlight").objectReferenceValue, Is.Not.Null);
                Assert.That(buttonData.FindProperty("actionText").objectReferenceValue, Is.Not.Null);
                Image progress = (Image)buttonData.FindProperty("buildProgress").objectReferenceValue;
                Assert.That(progress, Is.Not.Null);
                Assert.That(progress.type, Is.EqualTo(Image.Type.Filled));
            }
        }

        [Test]
        public void OrbitalPopup_OutsidePressDismissesWithoutCancelingTargetingAfterClose()
        {
            using OrbitalMenuFixture menu = new OrbitalMenuFixture();
            Vector2 popupCenter = ScreenCenter((RectTransform)menu.Popup.transform);
            Vector2 toggleCenter = ScreenCenter((RectTransform)menu.Toggle.transform);
            Vector2 outside = new Vector2(-10000, -10000);
            menu.Toggle.onClick.Invoke();
            menu.Pointer.Press(popupCenter);
            Assert.That(menu.Popup.activeSelf, Is.True);
            menu.Pointer.Press(toggleCenter);
            Assert.That(menu.Popup.activeSelf, Is.True, "The toggle handles its own click on release.");
            menu.Toggle.onClick.Invoke();
            Assert.That(menu.Popup.activeSelf, Is.False);

            menu.Toggle.onClick.Invoke();
            menu.Pointer.Press(outside);
            Assert.That(menu.Popup.activeSelf, Is.False);
            Assert.That(menu.Presenter.IsFocused, Is.False);

            menu.Targeting.Start(SuperWeaponType.IonCannon);
            menu.Pointer.Press(outside);
            Assert.That(menu.Targeting.Pending, Is.EqualTo(SuperWeaponType.IonCannon));
        }

        [Test]
        public void OrbitalRow_UsesTheProductionQueueThenAimsAReadyCharge()
        {
            using OrbitalMenuFixture menu = new OrbitalMenuFixture();
            const SuperWeaponType WEAPON = SuperWeaponType.IonCannon;
            Button purchase = (Button)menu.Row.FindProperty("button").objectReferenceValue;
            Button row = (Button)menu.Row.FindProperty("rowButton").objectReferenceValue;
            TMP_Text label = (TMP_Text)menu.Row.FindProperty("actionText").objectReferenceValue;
            TMP_Text detail = (TMP_Text)menu.Row.FindProperty("stateText").objectReferenceValue;
            Image progress = (Image)menu.Row.FindProperty("buildProgress").objectReferenceValue;
            menu.Toggle.onClick.Invoke();

            Assert.That(purchase.interactable, Is.True);
            Assert.That(detail.text, Is.EqualTo("Requires station level 3"));
            purchase.onClick.Invoke();
            Assert.That(menu.Production.Purchased, Is.Null);
            Assert.That(menu.Audio.Rejections, Is.EqualTo(1));
            menu.SecondStation.IsBuilt = true;
            menu.SecondStation.CurrentLevel = 3;
            menu.Presenter.Tick();
            Assert.That(purchase.gameObject.activeSelf, Is.True);
            Assert.That(purchase.interactable, Is.True);
            Assert.That(row.interactable, Is.False);
            string purchaseDetail = $"{menu.Request.FactionData.Price:N0} CR · {menu.Request.FactionData.BuildTime:0} s build";
            Assert.That(detail.text, Is.EqualTo(purchaseDetail));
            purchase.onClick.Invoke();
            Assert.That(menu.Production.Purchased, Is.Null);
            Assert.That(menu.Audio.Rejections, Is.EqualTo(2));
            menu.Economy.AddMoney(menu.Request.FactionData.Price);
            menu.Presenter.Tick();
            Assert.That(purchase.interactable, Is.True);
            Assert.That(detail.text, Is.EqualTo(purchaseDetail));
            Assert.That(label.text, Is.EqualTo("Buy charge"));
            row.onClick.Invoke();
            Assert.That(menu.Production.Purchased, Is.Null);
            purchase.onClick.Invoke();
            Assert.That(menu.Production.Purchased?.Kind, Is.EqualTo(UnitRequestKind.SuperWeapon));
            Assert.That(menu.Production.Purchased?.Key, Is.EqualTo((int)WEAPON));

            menu.Faction.Advance(menu.Request.FactionData.BuildTime / 2f);
            menu.Presenter.Tick();
            Assert.That(purchase.gameObject.activeSelf, Is.False);
            Assert.That(row.interactable, Is.True);
            Assert.That(progress.enabled, Is.True);
            Assert.That(progress.fillAmount, Is.EqualTo(0.5f).Within(0.001f));
            purchase.onClick.Invoke();
            Assert.That(menu.Production.Canceled, Is.Null);
            row.onClick.Invoke();
            Assert.That(menu.Confirmation.Content.Message, Does.Contain("Ion Cannon"));
            Assert.That(menu.InputLock.IsLocked, Is.True);
            Assert.That(menu.Production.Canceled, Is.Null);
            menu.BuildQueue.CancelBuilding(menu.Request.Id);
            menu.Confirmation.Answer(ConfirmPopupResult.Declined);
            Assert.That(menu.InputLock.IsLocked, Is.False);
            Assert.That(menu.Production.Canceled, Is.Null);
            row.onClick.Invoke();
            menu.Confirmation.Answer(ConfirmPopupResult.Accepted);
            Assert.That(menu.Production.Canceled?.RequestId, Is.EqualTo(menu.Request.Id));
            Assert.That(menu.InputLock.IsLocked, Is.False);
            Assert.That(menu.Economy.Money, Is.EqualTo(menu.InitialMoney + menu.Request.FactionData.Price));

            menu.Weapons.Restore(WEAPON, SuperWeaponState.Ready, 10f);
            menu.Presenter.Tick();
            Assert.That(purchase.gameObject.activeSelf, Is.False);
            Assert.That(row.interactable, Is.False);
            Assert.That(detail.text, Does.StartWith("Cooldown"));
            menu.Weapons.Tick(10f);
            menu.Presenter.Tick();
            Assert.That(row.interactable, Is.True);
            Assert.That(detail.text, Does.StartWith("Ready"));
            row.onClick.Invoke();
            Assert.That(menu.Popup.activeSelf, Is.False);
            Assert.That(menu.Targeting.Pending, Is.EqualTo(WEAPON));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ProductionQueueOrbitalCancel_RequiresConfirmationWhileBuildIsPending(bool acceptWhileQueued)
        {
            using OrbitalMenuFixture menu = new OrbitalMenuFixture();
            menu.Faction.QueueUnit(menu.Request);
            menu.Weapons.StartCharging(menu.Request.Key);
            int cancelNotifications = 0;
            menu.BuildQueue.Canceled += () => cancelNotifications++;

            menu.BuildQueue.CancelBuilding(menu.Request.Id);
            Assert.That(menu.Production.Canceled, Is.Null);
            Assert.That(cancelNotifications, Is.Zero);

            if (acceptWhileQueued)
            {
                menu.Confirmation.Answer(ConfirmPopupResult.Accepted);
                Assert.That(menu.Production.Canceled?.RequestId, Is.EqualTo(menu.Request.Id));
                Assert.That(cancelNotifications, Is.EqualTo(1));
            }
            else
            {
                menu.Faction.Advance(menu.Request.FactionData.BuildTime);
                Assert.That(menu.Confirmation.IsOpen, Is.False);
                Assert.That(menu.Production.Canceled, Is.Null);
                Assert.That(cancelNotifications, Is.Zero);
            }

            Assert.That(menu.InputLock.IsLocked, Is.False);
        }

        private static Vector2 ScreenCenter(RectTransform rect) =>
            RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));

        private sealed class OrbitalMenuFixture : IDisposable
        {
            private const string SHARED = "Assets/Settings/Data/Factions/Shared/";
            private readonly GameObject _root = PrefabUtility.LoadPrefabContents(
                "Assets/Prefabs/Ui/SkirmishGame/CoreGameUi.prefab");
            private readonly InputActionsProvider _inputActions = new InputActionsProvider();
            private readonly OrbitalConstructionCancellation _constructionCancellation;
            public int InitialMoney => 3200;
            public OrbitalPointer Pointer { get; } = new OrbitalPointer();
            public OrbitalInterfaceAudio Audio { get; } = new OrbitalInterfaceAudio();
            public SuperWeaponModel Weapons { get; } = new SuperWeaponModel();
            public SuperWeaponTargetingModel Targeting { get; } = new SuperWeaponTargetingModel();
            public SecondStationLevelModel SecondStation { get; } = new SecondStationLevelModel();
            public PlayerFactionModel Faction { get; }
            public EconomyModel Economy { get; }
            public OrbitalProduction Production { get; }
            public SuperWeaponUnitRequest Request { get; }
            public GameObject Popup { get; }
            public Button Toggle { get; }
            public SerializedObject Row { get; }
            public SuperWeaponPresenter Presenter { get; }
            public ShipBuildUiController BuildQueue { get; }
            public OrbitalConfirmation Confirmation { get; } = new OrbitalConfirmation();
            public InputLockService InputLock { get; }

            public OrbitalMenuFixture()
            {
                SuperWeaponsView view = (SuperWeaponsView)_root.GetComponent<CoreGameUi>().SuperWeaponsView;
                SerializedObject viewData = new SerializedObject(view);
                Popup = (GameObject)viewData.FindProperty("popup").objectReferenceValue;
                Toggle = (Button)viewData.FindProperty("orbitalButton").objectReferenceValue;
                SuperWeaponButton row = Array.Find(Popup.GetComponentsInChildren<SuperWeaponButton>(true),
                    candidate => candidate.WeaponType == SuperWeaponType.IonCannon);
                Row = new SerializedObject(row);

                FactionRoster roster = new FactionRoster(AssetDatabase.LoadAssetAtPath<FactionDefinition>(
                    "Assets/Settings/Data/Factions/Republic/RepublicFaction.asset"),
                    AssetDatabase.LoadAssetAtPath<MiningFacilityCatalog>(SHARED + "MiningFacilityCatalog.asset"),
                    AssetDatabase.LoadAssetAtPath<DefendPlatformCatalog>(SHARED + "DefendPlatformCatalog.asset"),
                    AssetDatabase.LoadAssetAtPath<SuperWeaponCatalog>(SHARED + "SuperWeaponCatalog.asset"));
                Faction = new PlayerFactionModel(null, roster);
                Economy = new EconomyModel(null, InitialMoney);
                UnitRequestFactory factory = new UnitRequestFactory();
                Request = factory.ConstructUnitRequest(roster.SuperWeapons[SuperWeaponType.IonCannon], SuperWeaponType.IonCannon);
                Production = new OrbitalProduction(this);
                PlayerCommandHandlers handlers = new PlayerCommandHandlers();
                PlayerRoster players = TestPlayers.CreateDuel();
                ILocalPlayer local = TestPlayers.CreateLocalPlayer(players);
                PlayerSlot owner = TestPlayers.Slot(players, local.Id);
                handlers.RegisterProduction(local.Id, Production);
                var commands = new AuthorityCommandSink(null, null, handlers, null);
                var battle = new SkirmishStateModel();
                battle.SetState(BattleState.Running);
                InputLock = new InputLockService(_inputActions);
                _constructionCancellation = new OrbitalConstructionCancellation(Faction, owner, battle, commands,
                    Confirmation, InputLock);
                _constructionCancellation.Initialize();
                BuildQueue = new ShipBuildUiController(null, null, commands, owner, Faction, null, null,
                    _constructionCancellation);
                Presenter = new SuperWeaponPresenter(
                    superWeaponFireService: null, viewProvider: new OrbitalViewProvider(view), routeNavigation: null,
                    abilities: new ShipAbilityService(null, null, null), localPlayer: local, uiService: null,
                    cancelRouter: new UiCancelRouter(null), tooltipService: null, faction: Faction, bindings: null,
                    battleState: null, model: Weapons, targeting: Targeting,
                    unitTargeting: new UnitActionTargetingModel(), data: null, interfaceAudio: Audio, interfaceData: null,
                    commands: commands, notifications: null, pointer: Pointer,
                    inputActions: _inputActions, roster: roster, requestFactory: factory, economy: Economy,
                    secondStation: SecondStation, productionAccess: new OrbitalProductionAccess(),
                    constructionCancellation: _constructionCancellation);
                Presenter.Activate(true);
            }

            public void Dispose()
            {
                Presenter.Activate(false);
                _constructionCancellation.LateDispose();
                InputLock.Dispose();
                DOTween.Kill(Row.FindProperty("background").objectReferenceValue);
                _inputActions.Dispose();
                PrefabUtility.UnloadPrefabContents(_root);
            }
        }

        private sealed class OrbitalConfirmation : IConfirmPopupService
        {
            private Action<ConfirmPopupResult> _answer;
            public AcceptPopupContent Content { get; private set; }
            public bool IsOpen => _answer != null;

            public PopupHandle ShowAccept(AcceptPopupContent content, Action<ConfirmPopupResult> answered)
            {
                Assert.That(IsOpen, Is.False);
                Content = content;
                _answer = answered;
                return new PopupHandle();
            }

            public void Close(PopupHandle handle) => Answer(ConfirmPopupResult.Dismissed);

            public void Answer(ConfirmPopupResult result)
            {
                Action<ConfirmPopupResult> answer = _answer;
                _answer = null;
                answer(result);
            }
        }

        private sealed class OrbitalInterfaceAudio : IInterfaceSfxService
        {
            public bool IsAnnouncing => false;
            public int Rejections { get; private set; }
            public void PlayRejected() => Rejections++;
            public void PlayAbilityTargetingCanceled() { }
            public void PlayAnnouncement(AudioClip clip) { }
            public bool TryPlayAnnouncement(AudioClip clip, AnnouncementPriority priority) => false;
            public bool TryPlayStationUpgrade(AudioClip clip, AudioClip newConstruction) => false;
        }

        private sealed class OrbitalProduction : IProductionCommandHandler
        {
            private readonly OrbitalMenuFixture _menu;
            public PurchaseUnitCommand? Purchased { get; private set; }
            public CancelPurchaseCommand? Canceled { get; private set; }

            public OrbitalProduction(OrbitalMenuFixture menu) => _menu = menu;

            public void Purchase(PurchaseUnitCommand command)
            {
                Purchased = command;
                Assert.That(_menu.Economy.TrySpend(_menu.Request.FactionData.Price), Is.True);
                _menu.Faction.QueueUnit(_menu.Request);
                _menu.Weapons.StartCharging(_menu.Request.Key);
            }

            public void Cancel(CancelPurchaseCommand command)
            {
                Canceled = command;
                Assert.That(_menu.Faction.TryCancelCurrentUnit(command.RequestId, out UnitRequest request), Is.True);
                _menu.Weapons.CancelCharging(_menu.Request.Key);
                _menu.Economy.AddMoney(request.FactionData.Price);
            }
        }

        private sealed class OrbitalProductionAccess : IProductionAccess
        {
            public bool CanProduce => true;
            public bool HasHostBase => true;
            public PlayerId ProductionHost => TestPlayers.Human;
            public bool TryGetProductionBase(out IEntity productionBase) => throw new NotSupportedException();
        }

        private sealed class OrbitalViewProvider : ISuperWeaponsViewProvider
        {
            public OrbitalViewProvider(ISuperWeaponsView view) => SuperWeaponsView = view;
            public ISuperWeaponsView SuperWeaponsView { get; }
        }

        private sealed class OrbitalPointer : IPointerInput
        {
            public event Action<Vector2> PrimaryPressed;
            public event Action<Vector2> PrimaryReleased { add { } remove { } }
            public Vector2 Position { get; private set; }
            public bool HasPosition => true;
            public int ClickCount => 1;

            public void Press(Vector2 position)
            {
                Position = position;
                PrimaryPressed(position);
            }
        }

        [Test]
        public void Data_DefinesPurchaseAndProfileForEverySuperWeapon()
        {
            SuperWeaponCatalog catalog = AssetDatabase.LoadAssetAtPath<SuperWeaponCatalog>(
                "Assets/Settings/Data/Factions/Shared/SuperWeaponCatalog.asset");
            SuperWeaponData data = AssetDatabase.LoadAssetAtPath<SuperWeaponData>(
                "Assets/Settings/Data/Models/SuperWeapons/SuperWeaponData.asset");
            foreach (SuperWeaponType type in Enum.GetValues(typeof(SuperWeaponType)))
            {
                FactionData purchase = catalog.Get(type);
                Assert.That(purchase.MaxCount, Is.EqualTo(1));
                Assert.That(purchase.Icon, Is.Not.Null);
                SuperWeaponProfile profile = data.GetProfile(type);
                Assert.That(profile.Weapon.ShotPrefab, Is.Not.Null, type.ToString());
                Assert.That(profile.Weapon.ShotsPerSalvo, Is.GreaterThan(0), type.ToString());
                Assert.That(Enum.IsDefined(typeof(WeaponType), profile.Weapon.WeaponType), Is.True, type.ToString());
            }
        }
    }
}
