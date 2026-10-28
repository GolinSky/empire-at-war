using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.Vision;
using NUnit.Framework;
using UnityEngine;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class EnemyShipAbilityControllerTests
    {
        [Test]
        public void LowShieldsAndNearbyPlayer_ActivatesDefensiveAbility()
        {
            GameObject playerView = new GameObject("Player");
            try
            {
                playerView.transform.position = Vector3.right * 5f;
                ShipAbilityDefinition definition = new ShipAbilityDefinition();
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                Type type = typeof(ShipAbilityDefinition);
                type.GetField("aiUse", flags).SetValue(definition, ShipAbilityAiUse.Defensive);
                type.GetField("duration", flags).SetValue(definition, 5f);
                type.GetField("recoveryDelay", flags).SetValue(definition, 5f);

                FakeCommand caster = new FakeCommand(definition);
                EntityLocator entities = new EntityLocator();
                entities.AddEntity(caster.Entity);
                entities.AddEntity(new FakeEntity(id: 2, owner: TestPlayers.Human,
                    health: new FakeHealth(playerView.transform, 1f), command: null));
                RecordingFactory factory = new RecordingFactory();
                PlayerRoster roster = TestPlayers.CreateDuel(EnemyAiDifficulty.UltraHard);
                ShipAbilityService service = new ShipAbilityService(factory, roster);
                EnemyShipAbilityController controller = new EnemyShipAbilityController(
                    entities, new FakeState(), roster, service, new VisionService(roster, fogEnabled: false), roster.Get(TestPlayers.Enemy));

                controller.Tick();

                Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
                Assert.That(factory.CreatedCount, Is.EqualTo(1));
                service.LateDispose();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerView);
            }
        }

        [Test]
        public void AbilityThatAddsUnits_DoesNotBreakTheCasterScan()
        {
            GameObject playerView = new GameObject("Player");
            try
            {
                playerView.transform.position = Vector3.right * 5f;
                ShipAbilityDefinition definition = new ShipAbilityDefinition();
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                Type type = typeof(ShipAbilityDefinition);
                type.GetField("aiUse", flags).SetValue(definition, ShipAbilityAiUse.Defensive);
                type.GetField("duration", flags).SetValue(definition, 5f);
                type.GetField("recoveryDelay", flags).SetValue(definition, 5f);

                FakeCommand caster = new FakeCommand(definition);
                EntityLocator entities = new EntityLocator();
                entities.AddEntity(caster.Entity);
                entities.AddEntity(new FakeEntity(id: 2, owner: TestPlayers.Human,
                    health: new FakeHealth(playerView.transform, 1f), command: null));
                // Like fighter reinforcements: the ability registers a new unit while the AI scans its casters.
                IEntity reinforcement = new FakeEntity(id: 3, owner: TestPlayers.Enemy,
                    health: new FakeHealth(null, 1f), command: null);
                RecordingFactory factory = new RecordingFactory(() => entities.AddEntity(reinforcement));
                PlayerRoster roster = TestPlayers.CreateDuel(EnemyAiDifficulty.UltraHard);
                ShipAbilityService service = new ShipAbilityService(factory, roster);
                EnemyShipAbilityController controller = new EnemyShipAbilityController(
                    entities, new FakeState(), roster, service, new VisionService(roster, fogEnabled: false), roster.Get(TestPlayers.Enemy));

                Assert.DoesNotThrow(controller.Tick);
                Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
                service.LateDispose();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerView);
            }
        }

        [Test]
        public void CasterRemovedBeforeItsTurn_IsSkipped()
        {
            GameObject playerView = new GameObject("Player");
            try
            {
                playerView.transform.position = Vector3.right * 5f;
                ShipAbilityDefinition definition = new ShipAbilityDefinition();
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                Type type = typeof(ShipAbilityDefinition);
                type.GetField("aiUse", flags).SetValue(definition, ShipAbilityAiUse.Defensive);
                type.GetField("duration", flags).SetValue(definition, 5f);
                type.GetField("recoveryDelay", flags).SetValue(definition, 5f);

                EntityLocator entities = new EntityLocator();
                // Five casters: the first tick works through four, the fifth waits for the next tick.
                List<LocatedCommand> casters = new List<LocatedCommand>();
                for (int id = 1; id <= 5; id++)
                {
                    LocatedCommand caster = new LocatedCommand(definition, entities, id);
                    casters.Add(caster);
                    entities.AddEntity(caster.OwnEntity);
                }
                entities.AddEntity(new FakeEntity(id: 10, owner: TestPlayers.Human,
                    health: new FakeHealth(playerView.transform, 1f), command: null));
                PlayerRoster roster = TestPlayers.CreateDuel(EnemyAiDifficulty.UltraHard);
                ShipAbilityService service = new ShipAbilityService(new RecordingFactory(), roster);
                EnemyShipAbilityController controller = new EnemyShipAbilityController(
                    entities, new FakeState(), roster, service, new VisionService(roster, fogEnabled: false), roster.Get(TestPlayers.Enemy));

                controller.Tick();
                entities.RemoveEntity(casters[4].OwnEntity);

                Assert.DoesNotThrow(controller.Tick);
                Assert.That(casters[4].Slots[0].State, Is.EqualTo(ShipAbilityState.Ready));
                service.LateDispose();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerView);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void RejectedPreferredTarget_TriesRemainingTargets(bool hasEligibleTarget)
        {
            GameObject targetView = new GameObject("Targets");
            try
            {
                targetView.transform.position = Vector3.right * 5f;
                ShipAbilityDefinition definition = new ShipAbilityDefinition();
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                Type type = typeof(ShipAbilityDefinition);
                type.GetField("aiUse", flags).SetValue(definition, ShipAbilityAiUse.Offensive);
                type.GetField("requiresEnemyTarget", flags).SetValue(definition, true);
                type.GetField("range", flags).SetValue(definition, 20f);
                type.GetField("duration", flags).SetValue(definition, 5f);
                FakeCommand caster = new FakeCommand(definition);
                EntityLocator entities = new EntityLocator();
                entities.AddEntity(caster.Entity);
                entities.AddEntity(new FakeEntity(new FakeHealth(targetView.transform, 1f)
                    { HullPercentage = 0.1f, ShipClass = ShipClass.Corvette },
                    null, TestPlayers.Human, 2));
                IEntity eligible = new FakeEntity(new FakeHealth(targetView.transform, 1f)
                    { ShipClass = hasEligibleTarget ? ShipClass.Frigate : ShipClass.Corvette },
                    null, TestPlayers.Human, 3);
                entities.AddEntity(eligible);
                TargetedFactory factory = new TargetedFactory();
                PlayerRoster roster = TestPlayers.CreateDuel(EnemyAiDifficulty.UltraHard);
                ShipAbilityService service = new ShipAbilityService(factory, roster);
                EnemyShipAbilityController controller = new EnemyShipAbilityController(
                    entities, new FakeState(), roster, service, new VisionService(roster, fogEnabled: false), roster.Get(TestPlayers.Enemy));

                controller.Tick();

                Assert.That(factory.Attempts, Is.EqualTo(new long[] { 2, 3 }));
                Assert.That(factory.ActivatedTarget, Is.SameAs(hasEligibleTarget ? eligible : null));
                Assert.That(caster.Slots[0].State, Is.EqualTo(hasEligibleTarget
                    ? ShipAbilityState.Active : ShipAbilityState.Ready));
                service.LateDispose();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetView);
            }
        }

        [Test]
        public void LockSFoils_StaysClosedDuringRetreatAndReopensAfterward()
        {
            ShipAbilityDefinition definition = new ShipAbilityDefinition();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = typeof(ShipAbilityDefinition);
            type.GetField("aiUse", flags).SetValue(definition, ShipAbilityAiUse.Escape);
            type.GetField("isToggle", flags).SetValue(definition, true);
            type.GetField("canCancel", flags).SetValue(definition, true);
            FakeCommand caster = new FakeCommand(definition, ShipAbilityId.LockSFoils);
            EntityLocator entities = new EntityLocator();
            entities.AddEntity(caster.Entity);
            RecordingFactory factory = new RecordingFactory();
            PlayerRoster roster = TestPlayers.CreateDuel(EnemyAiDifficulty.UltraHard);
            ShipAbilityService service = new ShipAbilityService(factory, roster);
            FakeState state = new FakeState { CurrentState = EnemyStrategicState.RetreatValue };
            EnemyShipAbilityController controller = new EnemyShipAbilityController(
                entities, state, roster, service, new VisionService(roster, fogEnabled: false), roster.Get(TestPlayers.Enemy));

            controller.Tick();
            service.Advance(60f);
            typeof(EnemyShipAbilityController).GetField("_timeLeft", flags).SetValue(controller, 0f);
            controller.Tick();
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Active));
            Assert.That(factory.StoppedCount, Is.Zero);

            state.CurrentState = EnemyStrategicState.HuntFleet;
            typeof(EnemyShipAbilityController).GetField("_timeLeft", flags).SetValue(controller, 0f);
            controller.Tick();
            Assert.That(factory.StoppedCount, Is.EqualTo(1));
            service.Advance(0f);
            Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Ready));
            typeof(EnemyShipAbilityController).GetField("_timeLeft", flags).SetValue(controller, 0f);
            controller.Tick();
            Assert.That(factory.CreatedCount, Is.EqualTo(1));
            service.LateDispose();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TargetVisibility_IsCheckedWhenCasterActs(bool visibleAtDecision)
        {
            GameObject targetView = new GameObject("Target");
            GameObject scoutView = new GameObject("Scout");
            try
            {
                targetView.transform.position = Vector3.right * 5f;
                ShipAbilityDefinition definition = new ShipAbilityDefinition();
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                Type type = typeof(ShipAbilityDefinition);
                type.GetField("aiUse", flags).SetValue(definition, ShipAbilityAiUse.Offensive);
                type.GetField("requiresEnemyTarget", flags).SetValue(definition, true);
                type.GetField("range", flags).SetValue(definition, 20f);
                type.GetField("duration", flags).SetValue(definition, 5f);
                FakeCommand caster = new FakeCommand(definition);
                EntityLocator entities = new EntityLocator();
                entities.AddEntity(caster.Entity);
                IEntity target = new FakeEntity(new FakeHealth(targetView.transform, 1f)
                    { ShipClass = ShipClass.Frigate }, null, TestPlayers.Human, 2);
                entities.AddEntity(target);
                TargetedFactory factory = new TargetedFactory();
                PlayerRoster roster = TestPlayers.CreateDuel(EnemyAiDifficulty.UltraHard);
                VisionService vision = new VisionService(roster);
                if (visibleAtDecision) vision.Register(TestPlayers.Enemy, scoutView.transform, 10f);
                ShipAbilityService service = new ShipAbilityService(factory, roster);
                EnemyShipAbilityController controller = new EnemyShipAbilityController(
                    entities, new FakeState(), roster, service, vision, roster.Get(TestPlayers.Enemy));

                typeof(EnemyShipAbilityController).GetMethod("StartDecision", flags).Invoke(controller, null);
                vision.Unregister(scoutView.transform);
                typeof(EnemyShipAbilityController).GetMethod("UseNextCasters", flags).Invoke(controller, null);
                Assert.That(factory.Attempts, Is.Empty);
                Assert.That(caster.Slots[0].State, Is.EqualTo(ShipAbilityState.Ready));

                vision.Register(TestPlayers.Enemy, scoutView.transform, 10f);
                typeof(EnemyShipAbilityController).GetMethod("StartDecision", flags).Invoke(controller, null);
                typeof(EnemyShipAbilityController).GetMethod("UseNextCasters", flags).Invoke(controller, null);
                Assert.That(factory.ActivatedTarget, Is.SameAs(target));
                service.LateDispose();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetView);
                UnityEngine.Object.DestroyImmediate(scoutView);
            }
        }

        private sealed class TargetedFactory : IShipAbilityFactory
        {
            public List<long> Attempts { get; } = new List<long>();
            public IEntity ActivatedTarget { get; private set; }

            public IShipAbility Create(ShipAbilityDefinition definition) => new TargetedAbility(this);

            private sealed class TargetedAbility : IPhasedShipAbility
            {
                private readonly TargetedFactory _factory;

                public bool IsComplete => false;
                public bool SurvivesCasterDeath => false;

                public TargetedAbility(TargetedFactory factory) => _factory = factory;

                public bool CanStart(IShipAbilityFacade caster, IEntity target)
                {
                    _factory.Attempts.Add(target.Id);
                    return target.HealthModel.ShipClass == ShipClass.Frigate;
                }

                public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition,
                    IEntity target) => _factory.ActivatedTarget = target;

                public void Advance(float deltaTime) { }
                public void Stop() { }
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

        private sealed class RecordingAbility : IShipAbility
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

        private sealed class FakeState : IEnemyAiStateProvider
        {
            public EnemyStrategicState CurrentState { get; set; } = EnemyStrategicState.Hold;
            public int ActiveShipCount => 1;
        }

        private sealed class FakeCommand : IShipAbilityFacade
        {
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

        private sealed class FakeHealth : IHealthModelObserver, IEntityTransformFacade
        {
            private readonly float _shieldPercentage;

            public event Action OnDestroy { add { } remove { } }

            public event Action OnValueChanged { add { } remove { } }

            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1f;
            public ShipClass ShipClass { get; set; } = ShipClass.Capital;
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
