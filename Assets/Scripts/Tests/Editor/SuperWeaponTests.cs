using EmpireAtWar.Entities.CoreGameUi;
using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.SuperWeapons.Ui;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Enemy;
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
                    TestPlayers.Human, 1);
                entities.AddEntity(station);
                RecordingSuperWeaponFireService fireService = new RecordingSuperWeaponFireService();
                SuperWeaponModel model = new SuperWeaponModel();
                // Charging, purchasing and economy are outside this firing regression.
                EnemySuperWeaponController controller = new EnemySuperWeaponController(
                    fireService, entities, vision, null, model, null, null, roster.Get(TestPlayers.Enemy));
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
            public bool CanTarget(PlayerId owner, IEntity target) => target.Owner != owner;
            public void Fire(SuperWeaponType type, IEntity target) => Target = target;
        }

        private sealed class StationTarget : IHealthModelObserver, IEntityTransformFacade
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
            public Transform Transform { get; }
            public ShipClass ShipClass => ShipClass.Structure;
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1000f;
            public float HullPercentage => 1f;
            public float Shields => 1000f;
            public float ShieldPercentage => 1f;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public bool HasLiveHardPoints => true;
            public bool HasShields => true;
            public PlayerId Owner => TestPlayers.Human;
            public StationTarget(Transform transform) => Transform = transform;
            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) =>
                Array.Empty<IHardPointModel>();
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
