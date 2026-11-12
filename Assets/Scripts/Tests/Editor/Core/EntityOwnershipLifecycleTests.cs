using System;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Squadrons;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Core")]
    public sealed class EntityOwnershipLifecycleTests
    {
        [Test]
        public void PoolEvents_RestoreSameIdentityAndStopAfterFinalDisposal()
        {
            EntityLocator entities = new EntityLocator();
            PoolEvents pool = new PoolEvents();
            Entity entity = new Entity(null, entities, Array.Empty<IEntityFacade>(), TestPlayers.Human, 17,
                new IPooledEntityEvents[] { pool }, Array.Empty<IHangarLaunchEvents>());

            entity.Initialize();
            Assert.That(entities.GetEntity(17), Is.SameAs(entity));
            pool.Despawn();
            Assert.That(entities.TryGetEntity(17, out _), Is.False);
            pool.Respawn();
            Assert.That(entities.GetEntity(17), Is.SameAs(entity));
            Assert.That(entity.Owner, Is.EqualTo(TestPlayers.Human));
            pool.Despawn();
            pool.Respawn();
            entity.LateDispose();
            pool.Despawn();
            pool.Respawn();
            Assert.That(entities.TryGetEntity(17, out _), Is.False);
        }

        [Test]
        public void HangarLaunch_GuardsOwningEntityAndStopsAfterFinalDisposal()
        {
            EntityLocator entities = new EntityLocator();
            HangarEvents localHangar = new HangarEvents();
            HangarEvents enemyHangar = new HangarEvents();
            Entity local = new Entity(null, entities, Array.Empty<IEntityFacade>(), TestPlayers.Human, 1,
                Array.Empty<IPooledEntityEvents>(), new IHangarLaunchEvents[] { localHangar });
            Entity enemy = new Entity(null, entities, Array.Empty<IEntityFacade>(), TestPlayers.Enemy, 2,
                Array.Empty<IPooledEntityEvents>(), new IHangarLaunchEvents[] { enemyHangar });
            SquadronSpy squadron = new SquadronSpy();
            local.Initialize();
            enemy.Initialize();

            localHangar.Launch(squadron);
            Assert.That(squadron.Guarded, Is.SameAs(local));
            Assert.That(squadron.GuardOffset, Is.EqualTo(Vector3.zero));
            enemyHangar.Launch(squadron);
            Assert.That(squadron.Guarded, Is.SameAs(enemy));
            local.LateDispose();
            localHangar.Launch(squadron);
            Assert.That(squadron.Guarded, Is.SameAs(enemy));
            Assert.That(squadron.GuardCount, Is.EqualTo(2));
            enemy.LateDispose();
        }

        private sealed class PoolEvents : IPooledEntityEvents
        {
            public event Action Respawned;
            public event Action Despawned;

            public void Respawn() => Respawned?.Invoke();
            public void Despawn() => Despawned?.Invoke();
        }

        private sealed class HangarEvents : IHangarLaunchEvents
        {
            public event Action<ISquadron> SquadronLaunched;

            public void Launch(ISquadron squadron) => SquadronLaunched?.Invoke(squadron);
        }

        private sealed class SquadronSpy : ISquadron
        {
            public event Action Released { add { } remove { } }
            public long EntityId => 0;
            public Vector3 WorldPosition => Vector3.zero;
            public IEntity Guarded { get; private set; }
            public Vector3 GuardOffset { get; private set; }
            public int GuardCount { get; private set; }

            public void Guard(IEntity friendly, Vector3 offset)
            {
                Guarded = friendly;
                GuardOffset = offset;
                GuardCount++;
            }

            public void AttackMoveTo(Vector3 worldPosition) => throw new NotSupportedException();
            public void Hunt() => throw new NotSupportedException();
        }
    }
}
