using System.Collections;
using System.Reflection;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.EnemyFaction.Controllers;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.TimerPoolWrapperService;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class EnemyFactionControllerLifecycleTests
    {
        private const BindingFlags PRIVATE_INSTANCE =
            BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void LateDispose_CancelsPendingBuildAndIsIdempotent()
        {
            EnemyFactionModel model =
                ScriptableObject.CreateInstance<EnemyFactionModel>();
            ReinforcementData reinforcementData =
                ScriptableObject.CreateInstance<ReinforcementData>();

            try
            {
                FactionData factionData = new FactionData();
                SetBackingField(factionData, nameof(FactionData.MaxCount), 1);
                SetBackingField(factionData, nameof(FactionData.BuildTime), 10);
                SetBackingField(factionData, nameof(FactionData.UnitCapacity), 1);
                SetBackingField(
                    reinforcementData,
                    nameof(ReinforcementData.MaxUnitCapacity),
                    10);

                TimerPoolWrapperService timerPool =
                    new TimerPoolWrapperService();
                EnemyUnitLimitModel unitLimitModel = new EnemyUnitLimitModel();
                TrackingEconomyProvider economyProvider =
                    new TrackingEconomyProvider();
                EnemyFactionController controller = new EnemyFactionController(
                    model,
                    null,
                    null,
                    null,
                    timerPool,
                    economyProvider,
                    null,
                    null,
                    unitLimitModel,
                    reinforcementData,
                    null);

                controller.Initialize();
                controller.Handle(
                    new ShipUnitRequest(factionData, ShipType.Venator));

                Assert.That(GetActiveTimerCount(timerPool), Is.EqualTo(1));
                Assert.That(unitLimitModel.CurrentUnitCapacity, Is.EqualTo(1));

                controller.LateDispose();
                controller.LateDispose();

                Assert.That(GetActiveTimerCount(timerPool), Is.Zero);
                Assert.That(unitLimitModel.CurrentUnitCapacity, Is.Zero);
                Assert.That(economyProvider.RemoveCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(model);
                Object.DestroyImmediate(reinforcementData);
            }
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

        private static int GetActiveTimerCount(
            TimerPoolWrapperService timerPool)
        {
            FieldInfo timerPoolField = typeof(TimerPoolWrapperService).GetField(
                "_timerPoolService",
                PRIVATE_INSTANCE);
            Assert.That(timerPoolField, Is.Not.Null);
            object timerPoolService = timerPoolField.GetValue(timerPool);
            FieldInfo activeTimersField = timerPoolService.GetType().GetField(
                "customCoroutines",
                PRIVATE_INSTANCE);
            Assert.That(activeTimersField, Is.Not.Null);
            ICollection activeTimers =
                activeTimersField.GetValue(timerPoolService) as ICollection;
            Assert.That(activeTimers, Is.Not.Null);
            return activeTimers.Count;
        }

        private sealed class TrackingEconomyProvider : IEconomyProvider
        {
            public int RemoveCount { get; private set; }

            public void AddProvider(IIncomeProvider incomeProvider)
            {
            }

            public void RemoveProvider(IIncomeProvider incomeProvider)
            {
                RemoveCount++;
            }

            public void RecalculateIncome(IIncomeProvider incomeProvider)
            {
            }
        }
    }
}
