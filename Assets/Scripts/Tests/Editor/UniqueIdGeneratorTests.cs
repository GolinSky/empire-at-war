using System;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.IdGeneration;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Core")]
    public sealed class UniqueIdGeneratorTests
    {
        [Test]
        public void Authority_AllocatesSequentialIds()
        {
            UniqueIdGenerator ids = new UniqueIdGenerator(new OfflineSession(TestPlayers.Human), new EntityLocator());
            ids.Initialize();

            Assert.That(new[] { ids.GenerateUniqueId(), ids.GenerateUniqueId(), ids.GenerateUniqueId() },
                Is.EqualTo(new long[] { 1, 2, 3 }));
        }

        [Test]
        public void RegisteredAndRestoredEntities_PushTheNextIdPastThem()
        {
            EntityLocator entities = new EntityLocator();
            entities.AddEntity(new FakeEntity(40));
            UniqueIdGenerator ids = new UniqueIdGenerator(new OfflineSession(TestPlayers.Human), entities);
            ids.Initialize();
            Assert.That(ids.GenerateUniqueId(), Is.EqualTo(41));

            entities.AddEntity(new FakeEntity(100));
            Assert.That(ids.GenerateUniqueId(), Is.EqualTo(101));
            ids.LateDispose();
        }

        [Test]
        public void NonAuthority_CannotAllocate()
        {
            UniqueIdGenerator ids = new UniqueIdGenerator(new ClientSession(), new EntityLocator());

            Assert.Throws<InvalidOperationException>(() => ids.GenerateUniqueId());
        }

        private sealed class ClientSession : IMatchSession
        {
            public bool IsOnline => true;
            public bool IsAuthority => false;
            public PlayerId LocalPlayerId => TestPlayers.Enemy;
        }

        private sealed class FakeEntity : IEntity
        {
            public FakeEntity(long id) => Id = id;

            public long Id { get; }
            public IHealthModelObserver HealthModel => null;
            public PlayerId Owner => TestPlayers.Human;

            public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade => throw new NotSupportedException();

            public bool TryGetFacade<TFacade>(out TFacade facade) where TFacade : IEntityFacade
            {
                facade = default;
                return false;
            }
        }
    }
}
