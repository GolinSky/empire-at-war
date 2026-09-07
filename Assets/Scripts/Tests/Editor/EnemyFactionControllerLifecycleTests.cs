using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.EnemyFaction.Controllers;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Patterns.ChainOfResponsibility;
using EmpireAtWar.Services.TimerPoolWrapperService;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Utilities.ScriptUtils.Time;

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
                UnityEngine.Object.DestroyImmediate(model);
                UnityEngine.Object.DestroyImmediate(reinforcementData);
            }
        }

        [Test]
        public void ScheduledBuildFailure_RefundsOnceAndDoesNotBlockLaterBuilds()
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
                TrackingPurchaseChain purchaseChain =
                    new TrackingPurchaseChain();
                EnemyFactionController controller = new EnemyFactionController(
                    model,
                    null,
                    null,
                    null,
                    timerPool,
                    new TrackingEconomyProvider(),
                    purchaseChain,
                    null,
                    unitLimitModel,
                    reinforcementData,
                    null);
                ShipUnitRequest request =
                    new ShipUnitRequest(factionData, ShipType.Venator);
                string unitId = $"{request.GetType().FullName}:{request.Id}";
                Assert.That(
                    unitLimitModel.TryReserve(unitId, 1, 1, 10),
                    Is.True);

                LogAssert.Expect(
                    LogType.Error,
                    new Regex(
                        @"^\[EnemyAI:Production\] Build failed for " +
                        @"ShipUnitRequest \(Venator\)\. Purchase refunded\."));
                ScheduleBuild(
                    controller,
                    request,
                    () => throw new InvalidOperationException("build failure"));
                GetOnlyActiveTimer(timerPool).Release(true);

                Assert.That(purchaseChain.RevertCount, Is.EqualTo(1));
                Assert.That(purchaseChain.LastReverted, Is.SameAs(request));
                Assert.That(unitLimitModel.CurrentUnitCapacity, Is.Zero);
                Assert.That(GetActiveTimerCount(timerPool), Is.Zero);
                Assert.That(GetPendingBuildCount(controller), Is.Zero);

                bool laterBuildExecuted = false;
                ScheduleBuild(controller, request, () => laterBuildExecuted = true);
                GetOnlyActiveTimer(timerPool).Release(true);

                Assert.That(laterBuildExecuted, Is.True);
                Assert.That(GetActiveTimerCount(timerPool), Is.Zero);
                Assert.That(GetPendingBuildCount(controller), Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(model);
                UnityEngine.Object.DestroyImmediate(reinforcementData);
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
            return GetActiveTimers(timerPool).Count;
        }

        private static ICollection GetActiveTimers(
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
            return activeTimers;
        }

        private static CustomCoroutine GetOnlyActiveTimer(
            TimerPoolWrapperService timerPool)
        {
            ICollection activeTimers = GetActiveTimers(timerPool);
            Assert.That(activeTimers.Count, Is.EqualTo(1));
            foreach (object activeTimer in activeTimers)
            {
                return (CustomCoroutine)activeTimer;
            }

            throw new InvalidOperationException("Expected one active timer.");
        }

        private static void ScheduleBuild(
            EnemyFactionController controller,
            UnitRequest request,
            Action buildAction)
        {
            MethodInfo method = typeof(EnemyFactionController).GetMethod(
                "ScheduleBuild",
                PRIVATE_INSTANCE);
            Assert.That(method, Is.Not.Null);
            method.Invoke(controller, new object[] { request, buildAction });
        }

        private static int GetPendingBuildCount(
            EnemyFactionController controller)
        {
            FieldInfo field = typeof(EnemyFactionController).GetField(
                "_pendingBuilds",
                PRIVATE_INSTANCE);
            Assert.That(field, Is.Not.Null);
            ICollection pendingBuilds = field.GetValue(controller) as ICollection;
            Assert.That(pendingBuilds, Is.Not.Null);
            return pendingBuilds.Count;
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

        private sealed class TrackingPurchaseChain : IPurchaseChain
        {
            public int RevertCount { get; private set; }
            public UnitRequest LastReverted { get; private set; }

            public IChainHandler<UnitRequest> SetNext(
                IChainHandler<UnitRequest> chainHandler)
            {
                return chainHandler;
            }

            public void Handle(UnitRequest request)
            {
            }

            public void Revert(UnitRequest result)
            {
                RevertCount++;
                LastReverted = result;
            }
        }
    }
}
