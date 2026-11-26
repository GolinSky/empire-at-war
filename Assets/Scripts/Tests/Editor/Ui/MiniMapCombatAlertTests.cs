using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Presenters.MiniMap;
using EmpireAtWar.Utils;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class MiniMapCombatAlertTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void LocalDamageStillPingsAndHealthSubscriptionsFollowEntityLifetime(bool shieldDamage)
        {
            var root = new GameObject("CombatAlertTarget");
            root.transform.position = new Vector3(25f, 0f, 60f);
            var data = ScriptableObject.CreateInstance<MiniMapData>();
            var entities = new EntityLocator();
            var local = new Unit(1, TestPlayers.Human, root.transform);
            var enemy = new Unit(2, TestPlayers.Enemy, root.transform);
            entities.AddEntity(local);
            entities.AddEntity(enemy);
            var state = new ReplayNotifier<BattleState>(BattleState.Running);
            var presenter = new MiniMapAlertPresenter(data, new Map(), entities,
                TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()), state);
            var alerts = new List<(MiniMapAlertType Type, Vector3 Position)>();
            data.OnAlertAdded += (type, position) => alerts.Add((type, position));
            try
            {
                presenter.Initialize();
                Assert.That(alerts, Is.Empty, "Existing health is a baseline, not an alert.");
                Assert.That(local.Health.Subscribers, Is.EqualTo(1));
                Assert.That(enemy.Health.Subscribers, Is.Zero);
                local.Health.Change(110f, 100f);
                enemy.Health.Change(0f, 0f);
                Assert.That(alerts, Is.Empty, "Healing and enemy damage do not produce local combat alerts.");

                local.Health.Change(shieldDamage ? 110f : 90f, shieldDamage ? 80f : 100f);
                Assert.That(alerts, Has.Count.EqualTo(1));
                Assert.That(alerts[0].Type, Is.EqualTo(MiniMapAlertType.Combat));
                Assert.That(alerts[0].Position, Is.EqualTo(root.transform.position));

                entities.RemoveEntity(local);
                Assert.That(local.Health.Subscribers, Is.Zero);
                entities.AddEntity(local);
                Assert.That(local.Health.Subscribers, Is.EqualTo(1));
                Assert.That(alerts, Has.Count.EqualTo(1), "Re-registration snapshots current health.");
                presenter.LateDispose();
                Assert.That(local.Health.Subscribers, Is.Zero);
                entities.RemoveEntity(local);
                entities.AddEntity(local);
                Assert.That(local.Health.Subscribers, Is.Zero, "Disposal detaches entity-added tracking.");
            }
            finally
            {
                presenter.LateDispose();
                UnityEngine.Object.DestroyImmediate(data);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private sealed class Map : IMapModelObserver
        {
            public Vector2Range SizeRange { get; } = new Vector2Range();
            public Map()
            {
                typeof(Vector2Range).BaseType.GetField("<Min>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(SizeRange, Vector2.zero);
                typeof(Vector2Range).BaseType.GetField("<Max>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(SizeRange, Vector2.one * 100f);
            }
            public Vector3 GetStationPosition(PlayerId owner) => throw new NotSupportedException();
        }

        private sealed class Unit : IEntity, IEntityTransformFacade
        {
            public long Id { get; }
            public PlayerId Owner { get; }
            public Transform Transform { get; }
            public Health Health { get; } = new Health();
            public IHealthModelObserver HealthModel => Health;
            public Unit(long id, PlayerId owner, Transform transform)
            { Id = id; Owner = owner; Transform = transform; }
            public bool TryGetFacade<T>(out T facade) where T : IEntityFacade
            { if (this is T result) { facade = result; return true; } facade = default; return false; }
            public T GetFacade<T>() where T : IEntityFacade => this is T result ? result : throw new InvalidOperationException();
        }

        private sealed class Health : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged;
            public int Subscribers => OnValueChanged == null ? 0 : OnValueChanged.GetInvocationList().Length;
            public float Hull { get; private set; } = 100f;
            public float Shields { get; private set; } = 100f;
            public void Change(float hull, float shields)
            { Hull = hull; Shields = shields; if (OnValueChanged != null) OnValueChanged(); }
            public ShipClass ShipClass => ShipClass.Capital;
            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<HardPointModel>();
            public float HullPercentage => Hull / 100f;
            public float ShieldPercentage => Shields / 100f;
            public bool IsDestroyed => Hull <= 0f;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public bool HasLiveHardPoints => true;
            public bool HasShields => true;
            public PlayerId Owner => TestPlayers.Human;
            public IHardPointModel[] GetShipUnits(HardPointType type) => Array.Empty<IHardPointModel>();
        }
    }
}
