using EmpireAtWar.Entities.CoreGameUi;
using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.SuperWeapons.Ui;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.SuperWeapons;
using EmpireAtWar.Services.Vision;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
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
                    System.Array.Empty<EmpireAtWar.Components.Hangar.IHangarLaunchEvents>());
                entities.AddEntity(station);
                RecordingSuperWeaponFireService fireService = new RecordingSuperWeaponFireService();
                SuperWeaponModel model = new SuperWeaponModel();
                // Charging, purchasing and economy are outside this firing regression.
                EnemySuperWeaponController controller = new EnemySuperWeaponController(
                    fireService, entities, vision, null, model, null, null, roster.Get(TestPlayers.Enemy), null,
                    LoadData(), roster);
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
            public event Action<SuperWeaponType, IEntity> Fired { add { } remove { } }
            public bool CanTarget(PlayerId owner, IEntity target) => target.Owner != owner;
            public void Fire(SuperWeaponType type, IEntity target) => Target = target;
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
                    Array.Empty<EmpireAtWar.Components.Hangar.IHangarLaunchEvents>()));
                vision.Register(TestPlayers.Enemy, scout.transform, 20f);
                RecordingSuperWeaponFireService fireService = new RecordingSuperWeaponFireService();
                SuperWeaponModel model = new SuperWeaponModel();
                EnemySuperWeaponController controller = new EnemySuperWeaponController(
                    fireService, entities, vision, null, model, null, null, roster.Get(TestPlayers.Enemy), null,
                    LoadData(), roster);
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

        [Test]
        public void Fire_DealsReducedDamageToSuperWeaponStunnedTargets()
        {
            GameObject view = new GameObject("Stunned target");
            GameObject audio = new GameObject("Test audio");
            EntityLocator entities = new EntityLocator();
            StationTarget health = new StationTarget(view.transform);
            Entity target = new Entity(health, entities, new IEntityFacade[] { health },
                TestPlayers.Human, 123, Array.Empty<IPooledEntityEvents>(),
                Array.Empty<EmpireAtWar.Components.Hangar.IHangarLaunchEvents>());
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

        private sealed class StationTarget : IHealthModelObserver, IEntityTransformFacade, IHealthFacade, ICombatModifiersFacade
        {
            public CombatModifiers Modifiers { get; } = new CombatModifiers();
            public int HitCount { get; private set; }
            public float LastDamage { get; private set; }
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
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
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
                Array.Empty<EmpireAtWar.Components.Hangar.IHangarLaunchEvents>());
            entities.AddEntity(target);
            SuperWeaponData data = AssetDatabase.LoadAssetAtPath<SuperWeaponData>(
                "Assets/Settings/Data/Models/SuperWeapons/SuperWeaponData.asset");
            SuperWeaponFireService fire = CreateFireService(entities, data, audio);
            RecordingAudio restoredAudio = new RecordingAudio();
            SuperWeaponFireService restored = CreateFireService(entities, data, audio, restoredAudio);
            try
            {
                origin.transform.position = new Vector3(3f, 5f, 7f);
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
                advance.Invoke(restored, new object[] { loaded, 0.11f });
                Assert.That(health.HitCount, Is.EqualTo(1));
                Assert.That(loaded.IsComplete, Is.False);
                advance.Invoke(restored, new object[] { loaded, 0.2f });
                Assert.That(health.HitCount, Is.EqualTo(2));
                Assert.That(loaded.IsComplete, Is.True);
                Assert.That(restoredAudio.Clips, Is.Empty, "Restoring and resolving already-fired impacts must not replay launch SFX.");
                advance.Invoke(restored, new object[] { loaded, 1f });
                Assert.That(health.HitCount, Is.EqualTo(2), "Loaded impacts must apply exactly once.");
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
                Array.Empty<EmpireAtWar.Components.Hangar.IHangarLaunchEvents>());
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
            IAudioService audioService = null, EmpireAtWar.ViewComponents.Weapon.BoltFlightBatch boltFlights = null)
        {
            AudioSource audioSource = audio.AddComponent<AudioSource>();
            ShipSfxSources sources = audio.AddComponent<ShipSfxSources>();
            typeof(ShipSfxSources).GetField("sfx", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(sources, new[] { audioSource });
            ShipSfxData sfx = AssetDatabase.LoadAssetAtPath<ShipSfxData>(
                "Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
            return new SuperWeaponFireService(entities, null, TestPlayers.CreateTeamGame(), data,
                new ImpactEffectPresenter(null, null), boltFlights, audioService, sfx, sources);
        }

        private sealed class RecordingAudio : EmpireAtWar.Mvc.Service, IAudioService
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
            var boltFlights = new EmpireAtWar.ViewComponents.Weapon.BoltFlightBatch();
            var entities = new EntityLocator();
            var health = new StationTarget(view.transform);
            var target = new Entity(health, entities, new IEntityFacade[] { health }, TestPlayers.Human, 123,
                Array.Empty<IPooledEntityEvents>(), Array.Empty<EmpireAtWar.Components.Hangar.IHangarLaunchEvents>());
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
        public void CoreGamePrefab_HasOneWiredButtonPerSuperWeapon()
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
                Assert.That(buttonData.FindProperty("pendingHighlight").objectReferenceValue, Is.Not.Null);
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
