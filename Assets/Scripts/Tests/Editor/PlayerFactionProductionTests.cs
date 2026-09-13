using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class PlayerFactionProductionTests
    {
        private const BindingFlags PRIVATE_INSTANCE =
            BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void CompleteThenQueueSameId_ReusesTheExistingPipeline()
        {
            PlayerFactionModel model = CreateModel();
            FactionData factionData = CreateFactionData(maxCount: 3, buildTime: 10);
            LevelUnitRequest first = new LevelUnitRequest(factionData, 1);
            LevelUnitRequest second = new LevelUnitRequest(factionData, 1);
            LevelUnitRequest third = new LevelUnitRequest(factionData, 1);

            model.QueueUnit(first);
            model.QueueUnit(second);

            model.Advance(10f);
            model.QueueUnit(third);

            IReadOnlyList<ProductionQueueSnapshot> snapshots =
                model.GetProductionQueueSnapshots();
            Assert.That(snapshots, Has.Count.EqualTo(1));
            Assert.That(snapshots[0].UnitRequest, Is.SameAs(second));
            Assert.That(snapshots[0].Count, Is.EqualTo(2));
        }

        [Test]
        public void Advance_CompletesSamePipelineInFifoOrderWithoutUi()
        {
            PlayerFactionModel model = CreateModel();
            FactionData factionData = CreateFactionData(maxCount: 2, buildTime: 5);
            LevelUnitRequest first = new LevelUnitRequest(factionData, 1);
            LevelUnitRequest second = new LevelUnitRequest(factionData, 1);
            List<UnitRequest> completedRequests = new List<UnitRequest>();
            model.OnUnitCompleted += completedRequests.Add;

            model.QueueUnit(first);
            model.QueueUnit(second);
            model.Advance(10f);

            Assert.That(completedRequests, Has.Count.EqualTo(2));
            Assert.That(completedRequests[0], Is.SameAs(first));
            Assert.That(completedRequests[1], Is.SameAs(second));
            Assert.That(model.GetProductionQueueSnapshots(), Is.Empty);
        }

        [Test]
        public void CanQueueUnit_RejectsEighthDistinctPipelineButAcceptsExistingId()
        {
            PlayerFactionModel model = CreateModel();
            FactionData factionData = CreateFactionData(maxCount: 2, buildTime: 10);

            for (int id = 1; id <= 7; id++)
            {
                model.QueueUnit(new LevelUnitRequest(factionData, id));
            }

            LevelUnitRequest eighthPipeline = new LevelUnitRequest(factionData, 8);
            LevelUnitRequest existingPipeline = new LevelUnitRequest(factionData, 1);

            Assert.That(model.CanQueueUnit(eighthPipeline), Is.False);
            Assert.That(model.CanQueueUnit(existingPipeline), Is.True);

            model.QueueUnit(existingPipeline);

            Assert.That(model.GetProductionQueueSnapshots(), Has.Count.EqualTo(7));
        }

        [Test]
        public void TryCancelCurrentUnit_ReturnsTheActiveRequestAndPreservesTheNextRequest()
        {
            PlayerFactionModel model = CreateModel();
            FactionData factionData = CreateFactionData(maxCount: 2, buildTime: 10);
            LevelUnitRequest first = new LevelUnitRequest(factionData, 1);
            LevelUnitRequest second = new LevelUnitRequest(factionData, 1);
            model.QueueUnit(first);
            model.QueueUnit(second);

            bool wasCancelled = model.TryCancelCurrentUnit(first.Id, out UnitRequest cancelled);

            IReadOnlyList<ProductionQueueSnapshot> snapshots =
                model.GetProductionQueueSnapshots();
            Assert.That(wasCancelled, Is.True);
            Assert.That(cancelled, Is.SameAs(first));
            Assert.That(snapshots, Has.Count.EqualTo(1));
            Assert.That(snapshots[0].UnitRequest, Is.SameAs(second));
            Assert.That(snapshots[0].Count, Is.EqualTo(1));
        }

        [Test]
        public void Advance_ZeroDurationRequest_CompletesImmediately()
        {
            PlayerFactionModel model = CreateModel();
            FactionData factionData = CreateFactionData(maxCount: 1, buildTime: 0);
            LevelUnitRequest request = new LevelUnitRequest(factionData, 1);
            UnitRequest completed = null;
            model.OnUnitCompleted += unitRequest => completed = unitRequest;

            model.QueueUnit(request);
            model.Advance(0f);

            Assert.That(completed, Is.SameAs(request));
            Assert.That(model.GetProductionQueueSnapshots(), Is.Empty);
        }

        private static PlayerFactionModel CreateModel()
        {
            return new PlayerFactionModel(null, FactionType.Republic);
        }

        private static FactionData CreateFactionData(int maxCount, int buildTime)
        {
            FactionData factionData = new FactionData();
            SetBackingField(factionData, nameof(FactionData.MaxCount), maxCount);
            SetBackingField(factionData, nameof(FactionData.BuildTime), buildTime);
            return factionData;
        }

        private static void SetBackingField<T>(
            object target,
            string propertyName,
            T value)
        {
            FieldInfo field = target.GetType().GetField(
                $"<{propertyName}>k__BackingField",
                PRIVATE_INSTANCE);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
