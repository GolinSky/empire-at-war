using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Combat")]
    public sealed class AttackMoveEngagementTests
    {
        private static readonly Func<IEntity, float> BY_ID = enemy => enemy.Id;
        private static readonly Func<IEntity, bool> ANY = enemy => true;

        [Test]
        public void PullBack_AppliesOnlyToSlotsBehindTheStoppedMemberAndLeaveClearsIt()
        {
            var engagement = new AttackMoveEngagement();
            int front = engagement.Join();
            int rear = engagement.Join();
            int side = engagement.Join();
            engagement.SetPullBack(front, 0f, 1000f, 0f, 0f, 300f);

            Assert.That(engagement.TryGetPullBack(rear, 0f, 800f, 0f, out float x, out float z), Is.True);
            Assert.That(x, Is.EqualTo(0f));
            Assert.That(z, Is.EqualTo(300f));
            Assert.That(engagement.TryGetPullBack(side, 200f, 1000f, 0f, out _, out _), Is.False,
                "A ship beside the stopped one keeps its slot and closes to its own range.");
            Assert.That(engagement.TryGetPullBack(front, 0f, 1000f, 0f, out _, out _), Is.False, "Not its own.");

            engagement.Leave(front);
            Assert.That(engagement.TryGetPullBack(rear, 0f, 800f, 0f, out _, out _), Is.False);
        }

        [Test]
        public void PullBack_TheLargestShortfallAheadWins()
        {
            var engagement = new AttackMoveEngagement();
            int small = engagement.Join();
            int large = engagement.Join();
            int rear = engagement.Join();
            engagement.SetPullBack(small, -100f, 1000f, 0f, 0f, 100f);
            engagement.SetPullBack(large, 100f, 1000f, 0f, 0f, 250f);
            engagement.ClearPullBack(small);
            engagement.SetPullBack(small, -100f, 1000f, 0f, 0f, 100f);

            Assert.That(engagement.TryGetPullBack(rear, 0f, 700f, 0f, out _, out float z), Is.True);
            Assert.That(z, Is.EqualTo(250f));
        }

        [Test]
        public void PullBack_AStopInAnotherHeightLayerHoldsNobodyBack()
        {
            var engagement = new AttackMoveEngagement();
            int dreadnought = engagement.Join();
            int frigate = engagement.Join();
            // A deep-flying dreadnought stopped 900 short at its long range.
            engagement.SetPullBack(dreadnought, 0f, 1000f, -391f, 0f, 900f);

            Assert.That(engagement.TryGetPullBack(frigate, 0f, 800f, 30f, out _, out _), Is.False,
                "Ships in other height layers pass over or under it.");
            Assert.That(engagement.TryGetPullBack(frigate, 0f, 800f, -391f + AttackMoveEngagement.SAME_LAYER_HEIGHT,
                out _, out _), Is.True);
        }

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
                picked.Add(engagement.SelectTarget(member, BY_ID, ANY));
            }

            Assert.That(picked, Is.EquivalentTo(enemies));
        }

        [Test]
        public void SelectTarget_PreferredEnemyOutranksNearerAndLessAttackedOnes()
        {
            AttackMoveEngagement engagement = new AttackMoveEngagement();
            FakeEntity near = new FakeEntity(1);
            FakeEntity preferred = new FakeEntity(2);
            int first = engagement.Join();
            int second = engagement.Join();
            engagement.Report(first, new IEntity[] { near, preferred });
            Func<IEntity, bool> isPreferred = enemy => enemy == preferred;

            Assert.That(engagement.HasPreferred(isPreferred), Is.True);
            Assert.That(engagement.SelectTarget(first, BY_ID, isPreferred), Is.SameAs(preferred));
            Assert.That(engagement.SelectTarget(second, BY_ID, isPreferred), Is.SameAs(preferred));
            Assert.That(engagement.HasPreferred(enemy => false), Is.False);
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

            Assert.That(engagement.SelectTarget(blind, BY_ID, ANY), Is.SameAs(enemy));
        }

        [Test]
        public void SelectTarget_SkipsIneligibleEnemiesAndReturnsNullWhenNoneQualify()
        {
            AttackMoveEngagement engagement = new AttackMoveEngagement();
            FakeEntity outside = new FakeEntity(1);
            FakeEntity inside = new FakeEntity(2);
            int member = engagement.Join();
            engagement.Report(member, new IEntity[] { outside, inside });

            Assert.That(engagement.SelectTarget(member, BY_ID, ANY, enemy => enemy == inside), Is.SameAs(inside));
            Assert.That(engagement.SelectTarget(member, BY_ID, ANY, enemy => false), Is.Null);
        }

        [Test]
        public void SelectTarget_InReachEnemyRanksBeforeAFarOneWithFewerAttackers()
        {
            AttackMoveEngagement engagement = new AttackMoveEngagement();
            FakeEntity inReach = new FakeEntity(1);
            FakeEntity far = new FakeEntity(2);
            int other = engagement.Join();
            int member = engagement.Join();
            engagement.Report(other, new IEntity[] { inReach, far });
            Func<IEntity, int> priority = enemy => enemy == inReach ? 1 : 0;
            Assert.That(engagement.SelectTarget(other, BY_ID, ANY, priority: priority), Is.SameAs(inReach));

            // Without the priority the far enemy wins on fewer attackers; with it the enemy in reach comes first.
            Assert.That(engagement.SelectTarget(member, BY_ID, ANY), Is.SameAs(far));
            Assert.That(engagement.SelectTarget(member, BY_ID, ANY, priority: priority), Is.SameAs(inReach));
            Func<IEntity, bool> isPreferred = enemy => enemy == far;
            Assert.That(engagement.SelectTarget(member, BY_ID, isPreferred, priority: priority), Is.SameAs(far),
                "The preferred class still comes first.");
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
            Assert.That(engagement.SelectTarget(member, BY_ID, ANY), Is.Null);
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
            Assert.That(engagement.SelectTarget(leaver, BY_ID, ANY), Is.SameAs(near));

            engagement.Leave(leaver);

            Assert.That(engagement.SelectTarget(stayer, BY_ID, ANY), Is.SameAs(near));
        }

        [Test]
        public void Contains_HoldsWhileAnyMemberReportsAndLeaveDropsReports()
        {
            AttackMoveEngagement engagement = new AttackMoveEngagement();
            FakeEntity enemy = new FakeEntity(1);
            int first = engagement.Join();
            int second = engagement.Join();
            engagement.Report(first, new IEntity[] { enemy });
            engagement.Report(second, new IEntity[] { enemy });

            engagement.Report(first, Array.Empty<IEntity>());
            Assert.That(engagement.Contains(enemy), Is.True);
            engagement.Leave(second);
            Assert.That(engagement.Contains(enemy), Is.False);
        }

        [Test]
        public void SelectTarget_ReselectingReleasesThePreviousClaim()
        {
            AttackMoveEngagement engagement = new AttackMoveEngagement();
            FakeEntity near = new FakeEntity(1);
            FakeEntity far = new FakeEntity(2);
            int member = engagement.Join();
            int other = engagement.Join();
            engagement.Report(member, new IEntity[] { near, far });

            Assert.That(engagement.SelectTarget(member, BY_ID, ANY), Is.SameAs(near));
            // Re-selecting drops the member's own claim, so it does not count against the nearest enemy.
            Assert.That(engagement.SelectTarget(member, BY_ID, ANY), Is.SameAs(near));
            Assert.That(engagement.SelectTarget(other, BY_ID, ANY), Is.SameAs(far));
        }

        private sealed class FakeEntity : IEntity
        {
            public long Id { get; }
            public FakeHealth Health { get; } = new FakeHealth();
            public IHealthModelObserver HealthModel => Health;
            public PlayerId Owner => TestPlayers.Enemy;

            public FakeEntity(long id) => Id = id;

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
            public event Action OnDestroy { add { } remove { } }

            public event Action OnValueChanged { add { } remove { } }

            public bool IsDestroyedValue { get; set; }
            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<HardPointModel>();
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
