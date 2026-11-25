using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Services.Networking;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Networking")]
    public sealed class ReplicatedFocusFireCountsTests
    {
        [Test]
        public void Apply_StoresTheLatestCounts_AndZeroOrRemovalForgetsTheTarget()
        {
            var counts = new ReplicatedFocusFireCounts();
            IEntity target = new EntityStub(7);

            counts.Apply(target.Id, 2, 5);
            Assert.That(counts.CountHolders(target, isSquadron: false), Is.EqualTo(2));
            Assert.That(counts.CountHolders(target, isSquadron: true), Is.EqualTo(5));

            counts.Apply(target.Id, 3, 1);
            Assert.That(counts.CountHolders(target, isSquadron: false), Is.EqualTo(3));

            counts.Apply(target.Id, 0, 0);
            Assert.That(counts.CountHolders(target, isSquadron: true), Is.Zero);

            counts.Apply(target.Id, 1, 1);
            counts.Remove(target.Id);
            Assert.That(counts.CountHolders(target, isSquadron: false), Is.Zero);
        }

        private sealed class EntityStub : IEntity
        {
            public EntityStub(long id) => Id = id;

            public long Id { get; }
            public EmpireAtWar.Models.Health.IHealthModelObserver HealthModel => null;
            public EmpireAtWar.Models.Players.PlayerId Owner => default;

            public bool TryGetFacade<TFacade>(out TFacade entityFacade) where TFacade : IEntityFacade
            {
                entityFacade = default;
                return false;
            }

            public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade =>
                throw new System.NotSupportedException();
        }
    }
}
