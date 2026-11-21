using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.Services.Vision;
using NUnit.Framework;
using UnityEngine;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class EnemyShipAbilityControllerTests
    {
        private ShipAbilityCatalog _catalog;

        [SetUp]
        public void SetUp() => _catalog = ScriptableObject.CreateInstance<ShipAbilityCatalog>();

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_catalog);

        // Default definitions have no auto-power role; the test abilities carry an auto-use rule instead.
        private EnemyShipAbilityController CreateController(IEntityLocator entities, PlayerRoster roster,
            ShipAbilityService service) =>
            new EnemyShipAbilityController(entities, service, roster.Get(TestPlayers.Enemy),
                new ShipAbilityAutoUse(null),
                new AutoPowerCombatSnapshot(entities, roster, service, null, null, _catalog), _catalog);

        private static ShipAbilityDefinition CreateRuleDefinition(ShipAbilitySettings settings,
            bool requiresEnemyTarget = false)
        {
            ShipAbilityDefinition definition = new ShipAbilityDefinition();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = typeof(ShipAbilityDefinition);
            type.GetField("settings", flags).SetValue(definition, settings);
            type.GetField("duration", flags).SetValue(definition, 5f);
            type.GetField("recoveryDelay", flags).SetValue(definition, 5f);
            type.GetField("requiresEnemyTarget", flags).SetValue(definition, requiresEnemyTarget);
            type.GetField("range", flags).SetValue(definition, 20f);
            return definition;
        }

        [TestCase(EnemyAiDifficulty.Easy)]
        [TestCase(EnemyAiDifficulty.UltraHard)]
        public void AutoUseRule_FiresOnTheFirstCheckAtEveryDifficulty(EnemyAiDifficulty difficulty)
        {
            FakeCommand caster = new FakeCommand(CreateRuleDefinition(new AlwaysSettings()));
            EntityLocator entities = new EntityLocator();
            entities.AddEntity(caster.Entity);
            RecordingFactory factory = new RecordingFactory();
            PlayerRoster roster = TestPlayers.CreateDuel(difficulty);
            ShipAbilityService service = new ShipAbilityService(factory, roster,
                new VisionService(roster, fogEnabled: false));
            EnemyShipAbilityController controller = CreateController(entities, roster, service);

            controller.Tick();

            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
            Assert.That(factory.CreatedCount, Is.EqualTo(1));
            service.LateDispose();
        }

        [Test]
        public void AbilityWithoutRoleOrRule_IsNeverUsed()
        {
            FakeCommand caster = new FakeCommand(CreateRuleDefinition(new BoostShieldPowerSettings()));
            EntityLocator entities = new EntityLocator();
            entities.AddEntity(caster.Entity);
            RecordingFactory factory = new RecordingFactory();
            PlayerRoster roster = TestPlayers.CreateDuel(EnemyAiDifficulty.UltraHard);
            ShipAbilityService service = new ShipAbilityService(factory, roster,
                new VisionService(roster, fogEnabled: false));
            EnemyShipAbilityController controller = CreateController(entities, roster, service);

            controller.Tick();

            Assert.That(factory.CreatedCount, Is.Zero);
            service.LateDispose();
        }

        [Test]
        public void AbilityThatAddsUnits_DoesNotBreakTheCasterScan()
        {
            FakeCommand caster = new FakeCommand(CreateRuleDefinition(new AlwaysSettings()));
            EntityLocator entities = new EntityLocator();
            entities.AddEntity(caster.Entity);
            // Like fighter reinforcements: the ability registers a new unit while the AI scans its casters.
            IEntity reinforcement = new FakeEntity(id: 3, owner: TestPlayers.Enemy,
                health: new FakeHealth(null, 1f), command: null);
            RecordingFactory factory = new RecordingFactory(() => entities.AddEntity(reinforcement));
            PlayerRoster roster = TestPlayers.CreateDuel(EnemyAiDifficulty.UltraHard);
            ShipAbilityService service = new ShipAbilityService(factory, roster,
                new VisionService(roster, fogEnabled: false));
            EnemyShipAbilityController controller = CreateController(entities, roster, service);

            Assert.DoesNotThrow(controller.Tick);
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
            service.LateDispose();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CasterRemovedBeforeItsTurn_IsSkipped(bool replaceEntity)
        {
            ShipAbilityDefinition definition = CreateRuleDefinition(new AlwaysSettings());
            EntityLocator entities = new EntityLocator();
            // Five casters: the first tick works through four, the fifth waits for the next tick.
            List<LocatedCommand> casters = new List<LocatedCommand>();
            for (int id = 1; id <= 5; id++)
            {
                LocatedCommand caster = new LocatedCommand(definition, entities, id);
                casters.Add(caster);
                entities.AddEntity(caster.OwnEntity);
            }
            PlayerRoster roster = TestPlayers.CreateDuel(EnemyAiDifficulty.UltraHard);
            ShipAbilityService service = new ShipAbilityService(new RecordingFactory(), roster,
                new VisionService(roster, fogEnabled: false));
            EnemyShipAbilityController controller = CreateController(entities, roster, service);

            controller.Tick();
            entities.RemoveEntity(casters[4].OwnEntity);
            if (replaceEntity)
                entities.AddEntity(new FakeEntity(new FakeHealth(null, 1f), null, TestPlayers.Enemy, 5));

            Assert.DoesNotThrow(controller.Tick);
            Assert.That(casters[4].Slots[0].State, Is.EqualTo(ShipAbilityState.Ready));
            service.LateDispose();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TargetedRule_TakesTheAutoPowerTarget(bool targetInRange)
        {
            GameObject targetView = new GameObject("Target");
            try
            {
                targetView.transform.position = Vector3.right * (targetInRange ? 5f : 100f);
                FakeCommand caster = new FakeCommand(CreateRuleDefinition(new AlwaysSettings(), requiresEnemyTarget: true));
                EntityLocator entities = new EntityLocator();
                entities.AddEntity(caster.Entity);
                IEntity target = new FakeEntity(new FakeHealth(targetView.transform, 1f) { ShipClass = ShipClass.Frigate },
                    null, TestPlayers.Human, 2);
                entities.AddEntity(target);
                caster.CurrentOrder = UnitOrderType.Attack;
                caster.AttackTarget = target;
                TargetedFactory factory = new TargetedFactory();
                PlayerRoster roster = TestPlayers.CreateDuel(EnemyAiDifficulty.UltraHard);
                ShipAbilityService service = new ShipAbilityService(factory, roster,
                    new VisionService(roster, fogEnabled: false));
                EnemyShipAbilityController controller = CreateController(entities, roster, service);

                controller.Tick();

                Assert.That(factory.ActivatedTarget, Is.SameAs(targetInRange ? target : null));
                Assert.That(caster.Slots[0].State, Is.EqualTo(targetInRange
                    ? ShipAbilityState.Active : ShipAbilityState.Ready));
                service.LateDispose();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetView);
            }
        }

        [Test]
        public void LockSFoils_CloseWhileTravellingAndReopenWhenTravelEnds()
        {
            ShipAbilityDefinition definition = CreateRuleDefinition(new LockSFoilsSettings());
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = typeof(ShipAbilityDefinition);
            type.GetField("isToggle", flags).SetValue(definition, true);
            type.GetField("canCancel", flags).SetValue(definition, true);
            type.GetField("duration", flags).SetValue(definition, 0f);
            type.GetField("recoveryDelay", flags).SetValue(definition, 0f);
            FakeCommand caster = new FakeCommand(definition, ShipAbilityId.LockSFoils);
            EntityLocator entities = new EntityLocator();
            entities.AddEntity(caster.Entity);
            RecordingFactory factory = new RecordingFactory();
            PlayerRoster roster = TestPlayers.CreateDuel(EnemyAiDifficulty.UltraHard);
            ShipAbilityService service = new ShipAbilityService(factory, roster,
                new VisionService(roster, fogEnabled: false));
            EnemyShipAbilityController controller = CreateController(entities, roster, service);

            controller.Tick();
            Assert.That(factory.CreatedCount, Is.Zero, "A unit that does not travel keeps its wings open.");

            caster.CurrentOrder = UnitOrderType.Move;
            typeof(EnemyShipAbilityController).GetField("_timeLeft", flags).SetValue(controller, 0f);
            controller.Tick();
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));

            caster.CurrentOrder = UnitOrderType.None;
            typeof(EnemyShipAbilityController).GetField("_timeLeft", flags).SetValue(controller, 0f);
            controller.Tick();
            Assert.That(factory.StoppedCount, Is.EqualTo(1));
            service.LateDispose();
        }

        [Serializable]
        private sealed class AlwaysSettings : ShipAbilitySettings, IShipAbilityAutoUseRule
        {
            public bool ShouldAutoActivate(ShipAbilityDependencies dependencies, IShipAbilityFacade caster) => true;

            // ShipAbilityFactoryTests creates every settings type and expects its matching *Ability type.
            public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) => new AlwaysAbility();
        }

        private sealed class AlwaysAbility : IShipAbility
        {
            public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target) { }
        }

        private sealed class TargetedFactory : IShipAbilityFactory
        {
            public int CreatedCount { get; private set; }
            public IEntity ActivatedTarget { get; private set; }

            public IShipAbility Create(ShipAbilityDefinition definition)
            {
                CreatedCount++;
                return new TargetedAbility(this);
            }

            private sealed class TargetedAbility : IShipAbility
            {
                private readonly TargetedFactory _factory;

                public TargetedAbility(TargetedFactory factory) => _factory = factory;

                public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition,
                    IEntity target) => _factory.ActivatedTarget = target;
            }
        }

        private sealed class RecordingFactory : IShipAbilityFactory
        {
            private readonly Action _onStart;

            public int CreatedCount { get; private set; }
            public int StoppedCount { get; private set; }

            public RecordingFactory() : this(() => { })
            {
            }

            public RecordingFactory(Action onStart)
            {
                _onStart = onStart;
            }

            public IShipAbility Create(ShipAbilityDefinition definition)
            {
                CreatedCount++;
                return new RecordingAbility(_onStart, () => StoppedCount++);
            }
        }

        private sealed class RecordingAbility : IStoppableShipAbility
        {
            private readonly Action _onStart;
            private readonly Action _onStop;

            public RecordingAbility(Action onStart, Action onStop)
            {
                _onStart = onStart;
                _onStop = onStop;
            }

            public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition,
                IEntity target) => _onStart();

            public void Stop() => _onStop();
        }

        private sealed class FakeCommand : IShipAbilityFacade, IUnitOrderObserverFacade
        {
            public UnitOrderType CurrentOrder { get; set; }
            public IEntity AttackTarget { get; set; }
            public IReadOnlyList<ShipAbilitySlot> Slots { get; }
            public CombatModifiers Modifiers { get; } = new CombatModifiers();
            public Vector3 WorldPosition => Vector3.zero;
            public IEntity Entity { get; }
            public IHealthModelObserver Health { get; }
            public float RadarRange => 20f;

            public FakeCommand(ShipAbilityDefinition definition,
                ShipAbilityId id = ShipAbilityId.BoostShieldPower)
            {
                Health = new FakeHealth(null, 0f);
                Entity = new FakeEntity(id: 1, owner: TestPlayers.Enemy, health: Health, command: this);
                Slots = new[] { new ShipAbilitySlot(id: id,
                    definition: definition, owner: this) };
            }
        }

        // Resolves its entity through the locator like ShipAbilityFacade, so it throws once the unit is removed.
        private sealed class LocatedCommand : IShipAbilityFacade
        {
            private readonly IEntityLocator _entities;
            private readonly long _id;

            public IReadOnlyList<ShipAbilitySlot> Slots { get; }
            public CombatModifiers Modifiers { get; } = new CombatModifiers();
            public Vector3 WorldPosition => Vector3.zero;
            public IEntity Entity => _entities.GetEntity(_id);
            public IEntity OwnEntity { get; }
            public IHealthModelObserver Health { get; }
            public float RadarRange => 20f;

            public LocatedCommand(ShipAbilityDefinition definition, IEntityLocator entities, long id)
            {
                _entities = entities;
                _id = id;
                Health = new FakeHealth(null, 0f);
                OwnEntity = new FakeEntity(id: id, owner: TestPlayers.Enemy, health: Health, command: this);
                Slots = new[] { new ShipAbilitySlot(id: ShipAbilityId.BoostShieldPower,
                    definition: definition, owner: this) };
            }
        }

        private sealed class FakeEntity : IEntity
        {
            private readonly IShipAbilityFacade _command;

            public long Id { get; }
            public IHealthModelObserver HealthModel { get; }
            public PlayerId Owner { get; }

            public FakeEntity(IHealthModelObserver health, IShipAbilityFacade command, PlayerId owner,
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

        private sealed class FakeHealth : IHealthModelObserver, IEntityTransformFacade, INavigationFootprintFacade
        {
            private readonly float _shieldPercentage;

            public event Action OnDestroy { add { } remove { } }

            public event Action OnValueChanged { add { } remove { } }

            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1f;
            public ShipClass ShipClass { get; set; } = ShipClass.Capital;
            public float NavigationRadius { get; set; }
            public bool HasLiveHardPoints => true;
            public float HullPercentage { get; set; } = 1f;
            public float Shields => _shieldPercentage;
            public float ShieldPercentage => _shieldPercentage;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public PlayerId Owner => TestPlayers.Human;
            public Transform Transform { get; }
            public bool HasShields => true;

            public FakeHealth(Transform transform, float shieldPercentage)
            {
                Transform = transform;
                _shieldPercentage = shieldPercentage;
            }

            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) =>
                Array.Empty<IHardPointModel>();
        }
    }
}
