using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.ShipAbilities;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
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
                entities.AddEntity(new FakeEntity(2, PlayerType.Player,
                    new FakeHealth(playerView.transform, 1f), null));
                RecordingFactory factory = new RecordingFactory();
                ShipAbilityService service = new ShipAbilityService(factory);
                EnemyShipAbilityController controller = new EnemyShipAbilityController(
                    entities, new FakeState(), new FakeGameModel(), service);

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

        private sealed class RecordingFactory : IShipAbilityFactory
        {
            public int CreatedCount { get; private set; }
            public IShipAbility Create(ShipAbilityDefinition definition)
            {
                CreatedCount++;
                return new RecordingAbility();
            }
        }

        private sealed class RecordingAbility : IShipAbility
        {
            public void Start(IShipAbilityCommand caster, ShipAbilityDefinition definition,
                IEntity target) { }
            public void Stop() { }
        }

        private sealed class FakeState : IEnemyAiStateProvider
        {
            public EnemyStrategicState CurrentState => EnemyStrategicState.Hold;
            public int ActiveShipCount => 1;
        }

        private sealed class FakeGameModel : IGameModelObserver
        {
            public EmpireAtWar.Entities.Planet.PlanetType PlanetType => default;
            public FactionType PlayerFactionType => default;
            public FactionType EnemyFactionType => default;
            public BattleVictoryCondition VictoryCondition => BattleVictoryCondition.DestroyEnemyFleet;
            public EnemyAiDifficulty EnemyDifficulty => EnemyAiDifficulty.UltraHard;
            public float StartingMoney => 1000f;
        }

        private sealed class FakeCommand : IShipAbilityCommand
        {
            public FakeCommand(ShipAbilityDefinition definition)
            {
                Health = new FakeHealth(null, 0f);
                Entity = new FakeEntity(1, PlayerType.Opponent, Health, this);
                Slots = new[] { new ShipAbilitySlot(ShipAbilityId.BoostShieldPower,
                    definition, this) };
            }

            public IReadOnlyList<ShipAbilitySlot> Slots { get; }
            public CombatModifiers Modifiers { get; } = new CombatModifiers();
            public Vector3 WorldPosition => Vector3.zero;
            public IEntity Entity { get; }
            public IHealthModelObserver Health { get; }
            public float RadarRange => 20f;
        }

        private sealed class FakeEntity : IEntity
        {
            private readonly IShipAbilityCommand _command;

            public FakeEntity(long id, PlayerType playerType, IHealthModelObserver health,
                IShipAbilityCommand command)
            {
                Id = id;
                PlayerType = playerType;
                HealthModel = health;
                _command = command;
            }

            public long Id { get; }
            public IModelObserver Model => null;
            public IHealthModelObserver HealthModel { get; }
            public PlayerType PlayerType { get; }

            public bool TryGetCommand<TCommand>(out TCommand entityCommand)
                where TCommand : IEntityCommand
            {
                if (_command is TCommand matching)
                {
                    entityCommand = matching;
                    return true;
                }
                entityCommand = default;
                return false;
            }
        }

        private sealed class FakeHealth : IHealthModelObserver
        {
            private readonly float _shieldPercentage;
            public FakeHealth(Transform transform, float shieldPercentage)
            {
                Transform = transform;
                _shieldPercentage = shieldPercentage;
            }

            public event Action OnDestroy;
            public event Action OnValueChanged;
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1f;
            public ShipClass ShipClass => ShipClass.Capital;
            public bool HasLiveHardPoints => true;
            public float HullPercentage => 1f;
            public float Shields => _shieldPercentage;
            public float ShieldPercentage => _shieldPercentage;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public PlayerType PlayerType => PlayerType.Player;
            public Transform Transform { get; }
            public bool HasShields => true;
            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) =>
                Array.Empty<IHardPointModel>();
        }
    }
}
