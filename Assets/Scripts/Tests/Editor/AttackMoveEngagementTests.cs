using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Ship;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class AttackMoveEngagementTests
    {
        private static readonly Func<IEntity, float> BY_ID = enemy => enemy.Id;

        [Test]
        public void SelectTarget_SpreadsMembersAcrossEnemyGroup()
        {
            AttackMoveEngagement engagement = new AttackMoveEngagement();
            FakeEntity[] enemies = { new FakeEntity(1), new FakeEntity(2), new FakeEntity(3) };
            HashSet<IEntity> picked = new HashSet<IEntity>();
            for (int i = 0; i < enemies.Length; i++)
            {
                int member = engagement.Join();
                engagement.Report(member, enemies);
                picked.Add(engagement.SelectTarget(member, BY_ID));
            }

            Assert.That(picked, Is.EquivalentTo(enemies));
        }

        [Test]
        public void SelectTarget_UsesEnemiesDetectedByOtherMembers()
        {
            AttackMoveEngagement engagement = new AttackMoveEngagement();
            FakeEntity enemy = new FakeEntity(1);
            int spotter = engagement.Join();
            int blind = engagement.Join();
            engagement.Report(spotter, new IEntity[] { enemy });
            engagement.Report(blind, Array.Empty<IEntity>());

            Assert.That(engagement.SelectTarget(blind, BY_ID), Is.SameAs(enemy));
        }

        [Test]
        public void Contains_DropsDestroyedAndUnreportedEnemies()
        {
            AttackMoveEngagement engagement = new AttackMoveEngagement();
            FakeEntity destroyed = new FakeEntity(1);
            FakeEntity lost = new FakeEntity(2);
            int member = engagement.Join();
            engagement.Report(member, new IEntity[] { destroyed, lost });

            destroyed.Health.IsDestroyedValue = true;
            Assert.That(engagement.Contains(destroyed), Is.False);
            engagement.Report(member, Array.Empty<IEntity>());
            Assert.That(engagement.Contains(lost), Is.False);
            Assert.That(engagement.SelectTarget(member, BY_ID), Is.Null);
        }

        [Test]
        public void Leave_ReleasesClaimAndContacts()
        {
            AttackMoveEngagement engagement = new AttackMoveEngagement();
            FakeEntity near = new FakeEntity(1);
            FakeEntity far = new FakeEntity(2);
            int leaver = engagement.Join();
            int stayer = engagement.Join();
            engagement.Report(leaver, new IEntity[] { near, far });
            engagement.Report(stayer, new IEntity[] { near, far });
            Assert.That(engagement.SelectTarget(leaver, BY_ID), Is.SameAs(near));

            engagement.Leave(leaver);

            Assert.That(engagement.SelectTarget(stayer, BY_ID), Is.SameAs(near));
        }

        private sealed class FakeEntity : IEntity
        {
            public FakeEntity(long id) => Id = id;
            public long Id { get; }
            public EmpireAtWar.Mvc.IModelObserver Model => null;
            public FakeHealth Health { get; } = new FakeHealth();
            public IHealthModelObserver HealthModel => Health;
            public PlayerId Owner => TestPlayers.Enemy;

            public TCommand GetFacade<TCommand>() where TCommand : IEntityFacade
            { TryGetFacade(out TCommand facade); return facade; }

            public bool TryGetFacade<TCommand>(out TCommand entityCommand)
                where TCommand : IEntityFacade
            {
                entityCommand = default;
                return false;
            }
        }

        private sealed class FakeHealth : IHealthModelObserver
        {
            public bool IsDestroyedValue { get; set; }
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1f;
            public ShipClass ShipClass => ShipClass.Capital;
            public bool HasLiveHardPoints => true;
            public float HullPercentage => 1f;
            public float Shields => 1f;
            public float ShieldPercentage => 1f;
            public bool IsDestroyed => IsDestroyedValue;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public PlayerId Owner => TestPlayers.Enemy;
            public bool HasShields => true;
            public IHardPointModel[] GetShipUnits(HardPointType type) =>
                Array.Empty<IHardPointModel>();
        }
    }
}
