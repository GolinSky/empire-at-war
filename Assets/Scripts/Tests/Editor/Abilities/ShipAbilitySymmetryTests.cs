using System;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.ViewComponents.Weapon;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Abilities")]
    public sealed class ShipAbilitySymmetryTests
    {
        private static readonly CombatStatModifier _testModifier =
            new CombatStatModifier(1.5f, 0.5f, 1.25f, 2f, 0.75f);

        [TestCase(ShipAbilityId.Invulnerability)]
        [TestCase(ShipAbilityId.BoostShieldPower)]
        [TestCase(ShipAbilityId.BoostEnginePower)]
        [TestCase(ShipAbilityId.BoostWeaponPower)]
        [TestCase(ShipAbilityId.Assault)]
        public void StatAbility_StartThenStop_RestoresCasterModifiers(ShipAbilityId id)
        {
            TestCommand caster = new TestCommand(id: 1, owner: TestPlayers.Human);
            IShipAbility ability = id switch
            {
                ShipAbilityId.Invulnerability =>
                    CreateSettings<InvulnerabilitySettings>("statModifier", _testModifier).CreateAbility(null),
                ShipAbilityId.BoostShieldPower =>
                    CreateSettings<BoostShieldPowerSettings>("statModifier", _testModifier).CreateAbility(null),
                ShipAbilityId.BoostEnginePower =>
                    CreateSettings<BoostEnginePowerSettings>("statModifier", _testModifier).CreateAbility(null),
                ShipAbilityId.BoostWeaponPower =>
                    CreateSettings<BoostWeaponPowerSettings>("statModifier", _testModifier).CreateAbility(null),
                ShipAbilityId.Assault => new AssaultAbility(
                    CreateSettings<AssaultSettings>("statModifier", _testModifier)),
                _ => throw new ArgumentOutOfRangeException(nameof(id))
            };

            ability.Start(caster, new ShipAbilityDefinition(), null);
            Assert.That(caster.Modifiers.DamageMultiplier, Is.EqualTo(1.5f));
            Assert.That(caster.Modifiers.WeaponPowerShotIntervalMultiplier,
                Is.EqualTo(id == ShipAbilityId.BoostWeaponPower ? 0.5f : 1f));
            ((IStoppableShipAbility)ability).Stop();
            AssertNeutral(caster.Modifiers);
        }

        [Test]
        public void BoostWeaponPower_PreservesOtherModifiersAndReplicaPersistence()
        {
            TestCommand caster = new TestCommand(id: 1, owner: TestPlayers.Human);
            CombatModifierHandle other = caster.Modifiers.Add(new CombatStatModifier(2f, 0.25f, 1f, 1f, 1f));
            BoostWeaponPowerSettings settings = CreateSettings<BoostWeaponPowerSettings>(
                "statModifier", new CombatStatModifier(1f, 0.5f, 0.5f, 0.5f, 1f));
            IStoppableShipAbility ability = (IStoppableShipAbility)settings.CreateAbility(null);

            Assert.That(ability, Is.InstanceOf<IReplicaPersistentShipAbility>());
            ability.Start(caster, new ShipAbilityDefinition(), null);
            Assert.That(caster.Modifiers.FireDelayMultiplier, Is.EqualTo(0.125f));
            Assert.That(caster.Modifiers.WeaponPowerShotIntervalMultiplier, Is.EqualTo(0.5f));
            Assert.That(caster.Modifiers.DamageMultiplier, Is.EqualTo(2f));
            ability.Stop();
            Assert.That(caster.Modifiers.FireDelayMultiplier, Is.EqualTo(0.25f));
            Assert.That(caster.Modifiers.WeaponPowerShotIntervalMultiplier, Is.EqualTo(1f));
            Assert.That(caster.Modifiers.DamageMultiplier, Is.EqualTo(2f));
            caster.Modifiers.Remove(other);
            AssertNeutral(caster.Modifiers);
            Assert.That(caster.Modifiers.HasTemporaryEffects, Is.False);
        }

        [Test]
        public void WeaponPowerShotInterval_TracksTemporaryEffectsAndNotifiesChanges()
        {
            CombatModifiers modifiers = new CombatModifiers();
            int changes = 0;
            modifiers.Changed += () => changes++;
            modifiers.SetWeaponPowerShotInterval(0.1f);
            Assert.That(modifiers.HasTemporaryEffects, Is.True);
            Assert.That(modifiers.FireDelayMultiplier, Is.EqualTo(1f));
            modifiers.SetWeaponPowerShotInterval(1f);
            Assert.That(modifiers.HasTemporaryEffects, Is.False);
            Assert.That(changes, Is.EqualTo(2));
        }

        [Test]
        public void ConcentrateFire_StartThenStop_RestoresAllAffectedAllies()
        {
            EntityLocator entities = new EntityLocator();
            TestCommand caster = new TestCommand(id: 1, owner: TestPlayers.Human);
            TestCommand ally = new TestCommand(id: 2, owner: TestPlayers.Human);
            entities.AddEntity(caster.Entity);
            entities.AddEntity(ally.Entity);
            ConcentrateFireSettings settings = CreateSettings<ConcentrateFireSettings>(
                "allyStatModifier", new CombatStatModifier(1.5f, 0.5f, 1f, 1f, 1f, shotIntervalMultiplier: 0.5f));
            SetField(settings, "commandRadius", 100f);
            IStoppableShipAbility ability = new ConcentrateFireAbility(settings: settings, entities: entities);

            ability.Start(caster, new ShipAbilityDefinition(), null);
            Assert.That(caster.Modifiers.DamageMultiplier, Is.EqualTo(1.5f));
            Assert.That(ally.Modifiers.DamageMultiplier, Is.EqualTo(1.5f));
            Assert.That(ally.Modifiers.FireDelayMultiplier, Is.EqualTo(0.5f));
            Assert.That(ally.Modifiers.ShotIntervalMultiplier, Is.EqualTo(0.5f));
            ability.Stop();
            AssertNeutral(caster.Modifiers);
            AssertNeutral(ally.Modifiers);
        }

        [Test]
        public void SensorJamming_CasterIonDisabled_Completes()
        {
            TestCommand caster = new TestCommand(id: 1, owner: TestPlayers.Human);
            ICompletableShipAbility ability = new SensorJammingAbility(
                new EntityLocator(), relations: null, settings: new SensorJammingSettings());

            ability.Start(caster, new ShipAbilityDefinition(), null);
            Assert.That(ability.IsComplete, Is.False);
            caster.Modifiers.SetIonDisabled(true);
            Assert.That(ability.IsComplete, Is.True);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void SensorJamming_IonDisable_CleansUpAndPreservesOtherEffects(bool overlappingJammer, bool ionPulse)
        {
            EntityLocator entities = new EntityLocator();
            IPlayerRelations relations = TestPlayers.CreateDuel();
            var dependencies = new ShipAbilityDependencies(entities, relations, null, null, null, null,
                new VisionService(relations, fogEnabled: false), new OfflineSession(TestPlayers.Human), null, null);
            ShipAbilityService service = new ShipAbilityService(new ShipAbilityFactory(dependencies), relations,
                new VisionService(relations, fogEnabled: false));
            ShipAbilityDefinition definition = new ShipAbilityDefinition();
            SetField(definition, "duration", 30f);
            SetField(definition, "recoveryDelay", 5f);
            SetField(definition, "range", 100f);
            SetField(definition, "settings", new SensorJammingSettings());
            TestCommand first = new TestCommand(TestPlayers.Human, 1, definition);
            TestCommand second = new TestCommand(TestPlayers.Human, 2, definition);
            GameObject targetView = new GameObject("Jammed target");
            TestCommand target = new TestCommand(TestPlayers.Enemy, 3, transform: targetView.transform);
            entities.AddEntity(first.Entity);
            entities.AddEntity(second.Entity);
            entities.AddEntity(target.Entity);
            target.Modifiers.AddFighterCoordination();
            try
            {
                Assert.That(service.TryActivate(first, ShipAbilityId.DauntlessSensorJamming, null), Is.True);
                if (overlappingJammer)
                    Assert.That(service.TryActivate(second, ShipAbilityId.DauntlessSensorJamming, null), Is.True);
                Assert.That(target.Modifiers.AccuracyMultiplier, Is.EqualTo(0.96f).Within(0.0001f));

                if (ionPulse) first.Modifiers.AddIonPulseDisable();
                else first.Modifiers.SetIonDisabled(true);
                service.Advance(0.1f);
                Assert.That(first.Slots[0].State, Is.EqualTo(ShipAbilityState.Recovering));
                Assert.That(first.Slots[0].TimeLeft, Is.EqualTo(5f));
                Assert.That(target.Modifiers.AccuracyMultiplier,
                    Is.EqualTo(overlappingJammer ? 0.96f : 1.2f).Within(0.0001f));

                if (overlappingJammer)
                {
                    Assert.That(second.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
                    second.Modifiers.SetIonDisabled(true);
                    service.Advance(0.1f);
                    Assert.That(second.Slots[0].State, Is.EqualTo(ShipAbilityState.Recovering));
                }
                Assert.That(target.Modifiers.AccuracyMultiplier, Is.EqualTo(1.2f));
                Assert.That(target.Modifiers.SpeedMultiplier, Is.EqualTo(1.15f));

                if (ionPulse) first.Modifiers.RemoveIonPulseDisable();
                else first.Modifiers.SetIonDisabled(false);
                service.Advance(5f);
                Assert.That(first.Slots[0].State, Is.EqualTo(ShipAbilityState.Ready));
                Assert.That(service.TryActivate(first, ShipAbilityId.DauntlessSensorJamming, null), Is.True);
                Assert.That(target.Modifiers.AccuracyMultiplier, Is.EqualTo(0.96f).Within(0.0001f));
                service.Clear();
                service.LateDispose();
                Assert.That(target.Modifiers.AccuracyMultiplier, Is.EqualTo(1.2f));
            }
            finally
            {
                service.Clear();
                target.Modifiers.RemoveFighterCoordination();
                UnityEngine.Object.DestroyImmediate(targetView);
            }
            Assert.That(target.Modifiers.HasTemporaryEffects, Is.False);
        }

        [Test]
        public void GravityWell_StartBlocksHostilesInRangeAndLeavesAlliesAndOutsideOpen()
        {
            IPlayerRelations relations = TestPlayers.CreateTeamGame();
            var areas = new BattleAreaService(relations);
            GameObject casterView = new GameObject("Interdictor");
            ShipAbilityDefinition definition = new ShipAbilityDefinition();
            SetField(definition, "range", 100f);
            GravityWellSettings settings = new GravityWellSettings();
            GravityWellEffects effects = CreateWellEffects(relations, settings, out GameObject wellPrefab);
            SetField(definition, "settings", settings);
            var caster = new TestCommand(TestPlayers.Human, 1, definition, casterView.transform, ShipAbilityId.Interdict);
            var ability = new GravityWellAbility(areas, effects, settings);
            try
            {
                ability.Start(caster, definition, null);

                Assert.That(areas.IsBlocked(TestPlayers.Enemy, new Vector3(50f, 0f, 0f)), Is.True);
                Assert.That(areas.IsBlocked(TestPlayers.SecondEnemy, new Vector3(0f, 0f, 99f)), Is.True);
                Assert.That(areas.IsBlocked(TestPlayers.Enemy, new Vector3(101f, 0f, 0f)), Is.False);
                Assert.That(areas.IsBlocked(TestPlayers.Human, Vector3.zero), Is.False);
                Assert.That(areas.IsBlocked(TestPlayers.Ally, Vector3.zero), Is.False);

                ability.Stop();
                Assert.That(areas.IsBlocked(TestPlayers.Enemy, Vector3.zero), Is.False);
            }
            finally
            {
                ability.Stop();
                UnityEngine.Object.DestroyImmediate(casterView);
                DestroyWellEffects(wellPrefab);
            }
        }

        [Test]
        public void GravityWell_MovingCasterMovesBlockedBubbleWithoutTicks()
        {
            IPlayerRelations relations = TestPlayers.CreateTeamGame();
            var areas = new BattleAreaService(relations);
            GameObject casterView = new GameObject("Interdictor");
            ShipAbilityDefinition definition = new ShipAbilityDefinition();
            SetField(definition, "range", 100f);
            GravityWellSettings settings = new GravityWellSettings();
            GravityWellEffects effects = CreateWellEffects(relations, settings, out GameObject wellPrefab);
            SetField(definition, "settings", settings);
            var caster = new TestCommand(TestPlayers.Human, 1, definition, casterView.transform, ShipAbilityId.Interdict);
            var ability = new GravityWellAbility(areas, effects, settings);
            try
            {
                ability.Start(caster, definition, null);
                casterView.transform.position = new Vector3(500f, 0f, 0f);

                Assert.That(areas.Get(BattleAreaLayer.SpawnBlock).Count, Is.EqualTo(1));
                Assert.That(areas.IsBlocked(TestPlayers.Enemy, Vector3.zero), Is.False);
                Assert.That(areas.IsBlocked(TestPlayers.Enemy, new Vector3(500f, 0f, 0f)), Is.True);
                Assert.That(areas.IsBlocked(TestPlayers.Enemy, new Vector3(601f, 0f, 0f)), Is.False);
            }
            finally
            {
                ability.Stop();
                UnityEngine.Object.DestroyImmediate(casterView);
                DestroyWellEffects(wellPrefab);
            }
        }

        [Test]
        public void GravityWell_ClearsOnIonDisableCasterDeathAndReplicaRecovery()
        {
            IPlayerRelations relations = TestPlayers.CreateDuel();
            var areas = new BattleAreaService(relations);
            GravityWellSettings wellSettings = new GravityWellSettings();
            GravityWellEffects effects = CreateWellEffects(relations, wellSettings, out GameObject wellPrefab);
            var dependencies = new ShipAbilityDependencies(new EntityLocator(), relations, null, null, null, null,
                new VisionService(relations, fogEnabled: false), new OfflineSession(TestPlayers.Human), areas, effects);
            var service = new ShipAbilityService(new ShipAbilityFactory(dependencies), relations,
                new VisionService(relations, fogEnabled: false));
            ShipAbilityDefinition definition = new ShipAbilityDefinition();
            SetField(definition, "duration", 30f);
            SetField(definition, "recoveryDelay", 60f);
            SetField(definition, "canCancel", true);
            SetField(definition, "range", 100f);
            SetField(definition, "settings", wellSettings);
            GameObject casterView = new GameObject("Interdictor");
            var caster = new TestCommand(TestPlayers.Human, 1, definition, casterView.transform, ShipAbilityId.Interdict);
            try
            {
                Assert.That(service.TryActivate(caster, ShipAbilityId.Interdict, null), Is.True);
                Assert.That(areas.Get(BattleAreaLayer.SpawnBlock).Count, Is.EqualTo(1));
                Assert.That(caster.Modifiers.IsAnchored, Is.True);
                Assert.That(caster.Emitting, Is.True);

                caster.Modifiers.SetIonDisabled(true);
                service.Advance(0.1f);
                Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Recovering));
                Assert.That(areas.Get(BattleAreaLayer.SpawnBlock), Is.Empty);
                Assert.That(caster.Modifiers.IsAnchored, Is.False);
                Assert.That(caster.Emitting, Is.False);
                caster.Modifiers.SetIonDisabled(false);

                service.RestoreReplicaState(caster, ShipAbilityId.Interdict, ShipAbilityState.Active, 15f, null);
                service.RestoreReplicaState(caster, ShipAbilityId.Interdict, ShipAbilityState.Active, 10f, null);
                Assert.That(areas.Get(BattleAreaLayer.SpawnBlock).Count, Is.EqualTo(1));
                Assert.That(caster.Modifiers.IsAnchored, Is.True);

                service.RestoreReplicaState(caster, ShipAbilityId.Interdict, ShipAbilityState.Recovering, 60f, null);
                Assert.That(areas.Get(BattleAreaLayer.SpawnBlock), Is.Empty);
                Assert.That(caster.Modifiers.IsAnchored, Is.False);

                service.RestoreReplicaState(caster, ShipAbilityId.Interdict, ShipAbilityState.Active, 12f, null);
                Assert.That(areas.Get(BattleAreaLayer.SpawnBlock).Count, Is.EqualTo(1));

                caster.FakeHealth.TriggerDestroy();
                Assert.That(areas.Get(BattleAreaLayer.SpawnBlock), Is.Empty);
                Assert.That(caster.Modifiers.IsAnchored, Is.False);
                service.Advance(0.1f);
                Assert.That(areas.Get(BattleAreaLayer.SpawnBlock), Is.Empty);
                Assert.That(areas.IsBlocked(TestPlayers.Enemy, Vector3.zero), Is.False);
            }
            finally
            {
                service.LateDispose();
                UnityEngine.Object.DestroyImmediate(casterView);
                DestroyWellEffects(wellPrefab);
            }
        }

        [Test]
        public void GravityWell_RingShowsOnlyWhileCasterIsSelectedAndAbilityActive()
        {
            IPlayerRelations relations = TestPlayers.CreateDuel();
            var areas = new BattleAreaService(relations);
            GameObject casterView = new GameObject("Interdictor");
            ShipAbilityDefinition definition = new ShipAbilityDefinition();
            SetField(definition, "range", 100f);
            GravityWellSettings settings = new GravityWellSettings();
            GravityWellEffects effects = CreateWellEffects(relations, settings, out GameObject wellPrefab);
            SetField(definition, "settings", settings);
            var caster = new TestCommand(TestPlayers.Human, 1, definition, casterView.transform, ShipAbilityId.Interdict);
            var ability = new GravityWellAbility(areas, effects, settings);
            SelectionModel selection = (SelectionModel)caster.Selection;
            try
            {
                ability.Start(caster, definition, null);
                GravityWellView view = GameObject.Find("Gravity Wells").GetComponentInChildren<GravityWellView>();
                LineRenderer ring = view.GetComponent<LineRenderer>();
                Assert.That(ring.enabled, Is.False);

                selection.IsSelected = true;
                Assert.That(ring.enabled, Is.True);
                selection.IsSelected = false;
                Assert.That(ring.enabled, Is.False);

                ability.Stop();
                Assert.That(view.gameObject.activeSelf, Is.False);
                ring.enabled = false;
                selection.IsSelected = true;
                Assert.That(ring.enabled, Is.False, "A released view must not follow selection again.");
            }
            finally
            {
                ability.Stop();
                UnityEngine.Object.DestroyImmediate(casterView);
                DestroyWellEffects(wellPrefab);
            }
        }

        [Test]
        public void GravityWell_EmitterPausesWhileOffCameraAndStopsWithTheAbility()
        {
            IPlayerRelations relations = TestPlayers.CreateDuel();
            GameObject cameraObject = new GameObject("Battle camera", typeof(UnityEngine.Camera));
            UnityEngine.Camera camera = cameraObject.GetComponent<UnityEngine.Camera>();
            camera.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var effects = new GravityWellEffects(relations, new OfflineSession(TestPlayers.Human), camera);
            var caster = new TestCommand(TestPlayers.Human, 1);
            try
            {
                caster.CullBounds = new Bounds(new Vector3(0f, 0f, 100f), Vector3.one * 10f);
                effects.StartEmitter(caster);
                Assert.That(caster.Emitting, Is.True);
                effects.Tick();
                Assert.That(caster.OnScreen, Is.True);

                caster.CullBounds = new Bounds(new Vector3(0f, 0f, -100f), Vector3.one * 10f);
                effects.Tick();
                Assert.That(caster.OnScreen, Is.False, "Behind the camera stops new particles.");

                effects.StopEmitter(caster);
                Assert.That(caster.Emitting, Is.False);
                caster.CullBounds = new Bounds(new Vector3(0f, 0f, 100f), Vector3.one * 10f);
                effects.Tick();
                Assert.That(caster.OnScreen, Is.False, "A stopped emitter is no longer tracked.");
            }
            finally
            {
                effects.LateDispose();
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static GravityWellEffects CreateWellEffects(IPlayerRelations relations, GravityWellSettings settings,
            out GameObject prefabObject)
        {
            prefabObject = new GameObject("GravityWellPrefab", typeof(LineRenderer));
            GravityWellView view = prefabObject.AddComponent<GravityWellView>();
            SetField(view, "ring", prefabObject.GetComponent<LineRenderer>());
            SetField(settings, "viewPrefab", view);
            return new GravityWellEffects(relations, new OfflineSession(TestPlayers.Human), null);
        }

        private static void DestroyWellEffects(GameObject prefabObject)
        {
            UnityEngine.Object.DestroyImmediate(prefabObject);
            GameObject root = GameObject.Find("Gravity Wells");
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        }

        private static TSettings CreateSettings<TSettings>(string modifierField, CombatStatModifier modifier)
            where TSettings : ShipAbilitySettings, new()
        {
            TSettings settings = new TSettings();
            SetField(settings, modifierField, modifier);
            return settings;
        }

        private static void SetField(object instance, string name, object value)
        {
            instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(instance, value);
        }

        private static void AssertNeutral(CombatModifiers modifiers)
        {
            Assert.That(modifiers.DamageMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.FireDelayMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.WeaponPowerShotIntervalMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.ShotIntervalMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.SpeedMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.ShieldRegenMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.DamageTakenMultiplier, Is.EqualTo(1f));
        }

        private sealed class TestCommand : IShipAbilityFacade, IAttackFacade, ICombatModifiersFacade,
            IGravityWellEmitterFacade, IEntitySelectionObserverFacade
        {
            public ISelectionModelObserver Selection { get; } = new SelectionModel();
            public bool Emitting { get; private set; }
            public bool OnScreen { get; private set; } = true;
            public Bounds CullBounds { get; set; } = new Bounds(Vector3.zero, Vector3.one * 10f);
            public void SetEmitting(bool emitting) => Emitting = emitting;
            public void SetOnScreen(bool onScreen) => OnScreen = onScreen;
            public IReadOnlyList<ShipAbilitySlot> Slots { get; } = Array.Empty<ShipAbilitySlot>();
            public CombatModifiers Modifiers { get; } = new CombatModifiers();
            public Vector3 WorldPosition => Vector3.zero;
            public IEntity Entity { get; }
            public FakeHealth FakeHealth { get; }
            public IHealthModelObserver Health => FakeHealth;
            public float RadarRange => 100f;
            public float NavigationRadius => 1f;

            public TestCommand(PlayerId owner, long id, ShipAbilityDefinition definition = null,
                Transform transform = null, ShipAbilityId abilityId = ShipAbilityId.DauntlessSensorJamming)
            {
                FakeHealth = new FakeHealth { Transform = transform };
                Entity = new TestEntity(id: id, owner: owner, health: FakeHealth, command: this);
                if (definition != null)
                    Slots = new[] { new ShipAbilitySlot(this, definition, abilityId) };
            }

            public void Attack(IEntity target, Vector3 formationOffset, GroupPace pace) { }
        }

        private sealed class TestEntity : IEntity
        {
            private readonly TestCommand _command;

            public long Id { get; }
            public IHealthModelObserver HealthModel { get; }
            public PlayerId Owner { get; }

            public TestEntity(IHealthModelObserver health, TestCommand command, PlayerId owner,
                long id)
            {
                Id = id;
                Owner = owner;
                HealthModel = health;
                _command = command;
            }

            public TCommand GetFacade<TCommand>() where TCommand : IEntityFacade
            { TryGetFacade(out TCommand facade); return facade; }

            public bool TryGetFacade<TCommand>(out TCommand entityCommand)
                where TCommand : IEntityFacade
            { if (HealthModel is TCommand transformFacade) { entityCommand = transformFacade; return true; }
                if (_command is TCommand matching)
                {
                    entityCommand = matching;
                    return true;
                }
                entityCommand = default;
                return false;
            }
        }

        private sealed class FakeHealth : IHealthModelObserver, IEntityTransformFacade
        {
            public event Action OnDestroy;

            public event Action OnValueChanged { add { } remove { } }

            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels { get; } = new[]
                { new HardPointModel(HardPointType.SensorArray, 0) };
            public float Hull => 1f;
            public ShipClass ShipClass => ShipClass.Capital;
            public bool HasLiveHardPoints => true;
            public float HullPercentage => 1f;
            public float Shields => 1f;
            public float ShieldPercentage => 1f;
            public bool IsDestroyed { get; private set; }
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public PlayerId Owner => TestPlayers.Human;
            public Transform Transform { get; set; }
            public bool HasShields => true;

            public void TriggerDestroy()
            {
                IsDestroyed = true;
                OnDestroy?.Invoke();
            }

            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) =>
                Array.Empty<IHardPointModel>();
        }
    }
}
