using System;
using System.Reflection;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Utils;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class EnemyDecisionSchedulingTests
    {
        [Test]
        public void SquadronPlayers_StartAtDifferentOffsets_AndPausePreservesCountdown()
        {
            PlayerRoster roster = TestPlayers.CreateTeamGame();
            var battleState = new ReplayNotifier<BattleState>(BattleState.Paused);
            var first = Create(roster.Get(TestPlayers.Enemy), battleState);
            var second = Create(roster.Get(TestPlayers.SecondEnemy), battleState);
            first.Initialize();
            second.Initialize();
            try
            {
                float firstDelay = Delay(first);
                float secondDelay = Delay(second);
                Assert.That(firstDelay, Is.InRange(0f, 3f));
                Assert.That(secondDelay, Is.InRange(0f, 3f));
                Assert.That(firstDelay, Is.Not.EqualTo(secondDelay));
                first.Tick();
                second.Tick();
                Assert.That(Delay(first), Is.EqualTo(firstDelay));
                Assert.That(Delay(second), Is.EqualTo(secondDelay));
                battleState.Set(BattleState.Running);
                battleState.Set(BattleState.Paused);
                Assert.That(Delay(first), Is.EqualTo(firstDelay), "Resume must not synchronize players again.");
            }
            finally
            {
                first.LateDispose();
                second.LateDispose();
            }
        }

        [Test]
        public void DiagnosticRates_AreIndependentAndExpireAfterAnIdleWindow()
        {
            var periodic = new EnemyDecisionDiagnostics("Test.Enemy.Periodic");
            var launches = new EnemyDecisionDiagnostics("Test.Enemy.Launch");
            periodic.Record();
            launches.Record();
            launches.Record();
            periodic.Tick(1f);
            launches.Tick(1f);
            Assert.That(periodic.PerSecond, Is.EqualTo(1f));
            Assert.That(launches.PerSecond, Is.EqualTo(2f));
            periodic.Tick(1f);
            launches.Tick(1f);
            Assert.That(periodic.PerSecond, Is.Zero);
            Assert.That(launches.PerSecond, Is.Zero);
            Assert.That(launches.Count, Is.EqualTo(2));
        }

        [TestCase(0f, 0.5f)]
        [TestCase(0.1f, 0.4f)]
        [TestCase(0.25f, 0.25f)]
        public void ShipWorldChanges_RespectRemainingMinimumDecisionGap(float elapsed, float expectedDelay)
        {
            EnemyUnitCommander commander = CreateShipCommander();
            SetShipClock(commander, elapsed, 4f);

            NotifyWorldChange(commander);

            Assert.That(ShipField(commander, "_decisionTimer"), Is.EqualTo(expectedDelay).Within(0.0001f));
        }

        [Test]
        public void ShipWorldChanges_BurstsDoNotPostponePendingDecisionOrEarlierNormalTimer()
        {
            EnemyUnitCommander commander = CreateShipCommander();
            SetShipClock(commander, 0f, 4f);
            NotifyWorldChange(commander);
            for (int i = 1; i <= 4; i++)
            {
                float elapsed = i * 0.1f;
                float remaining = 0.5f - elapsed;
                SetShipClock(commander, elapsed, remaining);
                NotifyWorldChange(commander);
                NotifyWorldChange(commander); // Ship and entity notifications can describe the same spawn.
                Assert.That(ShipField(commander, "_decisionTimer"), Is.EqualTo(remaining).Within(0.0001f),
                    "Repeated events must not turn the decision deadline into a sliding debounce.");
            }

            SetShipClock(commander, 0.25f, 0.05f);
            NotifyWorldChange(commander);
            Assert.That(ShipField(commander, "_decisionTimer"), Is.EqualTo(0.05f),
                "An earlier normal decision or initial stagger must never be delayed by an event.");
        }

        [Test]
        public void ShipWorldChanges_InitialAndQuietReactionsKeepOwnerDelay()
        {
            EnemyUnitCommander commander = CreateShipCommander();
            typeof(EnemyUnitCommander).GetField("_decisionTimer", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(commander, 4f);
            NotifyWorldChange(commander);
            float initialDelay = ShipField(commander, "_decisionTimer");
            Assert.That(initialDelay, Is.InRange(0.1f, 0.2f), "The first decision has no previous gap to enforce.");

            SetShipClock(commander, 2f, 4f);
            NotifyWorldChange(commander);
            Assert.That(ShipField(commander, "_decisionTimer"), Is.EqualTo(initialDelay));
        }

        [Test]
        public void ShipWorldChanges_PausePreservesElapsedGapAndCountdown()
        {
            EnemyUnitCommander commander = CreateShipCommander();
            SetShipClock(commander, 0.1f, 0.4f);
            commander.UpdateState(BattleState.Paused);
            commander.Tick();
            Assert.That(ShipField(commander, "_timeSinceDecision"), Is.EqualTo(0.1f));
            Assert.That(ShipField(commander, "_decisionTimer"), Is.EqualTo(0.4f));
            commander.UpdateState(BattleState.Running);
            NotifyWorldChange(commander);
            Assert.That(ShipField(commander, "_decisionTimer"), Is.EqualTo(0.4f));
        }

        private static EnemyUnitCommander CreateShipCommander()
        {
            PlayerSlot owner = TestPlayers.CreateDuel().Get(TestPlayers.Enemy);
            var builder = new EnemyStrategicContextBuilder(null, null, Array.Empty<IEnemyCaptureObjectiveSource>(),
                null, null, null, null, new TeamIntelRegistry(), null, owner, null, null, null, null, null, null);
            return new EnemyUnitCommander(null, null, null, null, null, null, builder, null,
                owner, null, null, null);
        }

        private static void SetShipClock(EnemyUnitCommander commander, float elapsed, float remaining)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(EnemyUnitCommander).GetField("_timeSinceDecision", flags).SetValue(commander, elapsed);
            typeof(EnemyUnitCommander).GetField("_decisionTimer", flags).SetValue(commander, remaining);
        }

        private static float ShipField(EnemyUnitCommander commander, string name) =>
            (float)typeof(EnemyUnitCommander).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(commander);

        private static void NotifyWorldChange(EnemyUnitCommander commander) =>
            typeof(EnemyUnitCommander).GetMethod("HandleWorldChanged", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(commander, null);

        private static float Delay(EnemySquadronCommander commander) =>
            (float)typeof(EnemySquadronCommander).GetField("_decisionTimer",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(commander);

        private static EnemySquadronCommander Create(PlayerSlot owner, ReplayNotifier<BattleState> state) =>
            new EnemySquadronCommander(Array.Empty<IEnemyCaptureObjectiveSource>(), null, null, null,
                null, null, null, new TeamIntelRegistry(), owner, null, null, state);

        [Test]
        public void Scheduler_RunsDueUnitsOldestFirst_AndRespectsBudgetUntilCompletion()
        {
            var battleState = new ReplayNotifier<BattleState>(BattleState.Running);
            double clockMs = 0.0;
            var scheduler = new EnemyAiScheduler(battleState, () => clockMs)
            {
                FrameBudgetMs = 1f
            };
            scheduler.Initialize();
            var executed = new System.Collections.Generic.List<string>();

            scheduler.Register(TestPlayers.Enemy, 2f, 0.5f, () =>
            {
                executed.Add("first");
                clockMs += 1.5;
            });
            scheduler.Register(TestPlayers.SecondEnemy, 2f, 0.2f, () =>
            {
                executed.Add("second");
                clockMs += 1.5;
            });

            scheduler.Step(1f);
            Assert.That(executed, Is.EqualTo(new[] { "second" }), "Oldest due unit must run first and exhaust budget.");

            scheduler.Step(0.1f);
            Assert.That(executed, Is.EqualTo(new[] { "second", "first" }), "Deferred unit must complete on the next frame.");

            scheduler.LateDispose();
        }

        [Test]
        public void Scheduler_StationCriticalJumpsQueue_AndPauseFreezesDueTimes()
        {
            var battleState = new ReplayNotifier<BattleState>(BattleState.Running);
            double clockMs = 0.0;
            var scheduler = new EnemyAiScheduler(battleState, () => clockMs)
            {
                FrameBudgetMs = 0.5f
            };
            scheduler.Initialize();
            var executed = new System.Collections.Generic.List<string>();

            var normal = scheduler.Register(TestPlayers.Enemy, 2f, 0.1f, () =>
            {
                executed.Add("normal");
                clockMs += 1.0;
            });
            var critical = scheduler.Register(TestPlayers.SecondEnemy, 2f, 0.5f, () =>
            {
                executed.Add("critical");
                clockMs += 1.0;
            }, isCritical: () => true);

            scheduler.Step(1f);
            Assert.That(executed, Is.EqualTo(new[] { "critical" }), "StationCritical unit must jump ahead of older normal unit.");

            battleState.Set(BattleState.Paused);
            float remainingBeforePause = scheduler.GetRemainingDelay(critical);
            scheduler.Step(5f);
            Assert.That(scheduler.GetRemainingDelay(critical), Is.EqualTo(remainingBeforePause));
            Assert.That(executed.Count, Is.EqualTo(1));
            Assert.That(scheduler.GetRemainingDelay(normal), Is.Zero);

            scheduler.LateDispose();
        }

        [Test]
        public void Scheduler_StarvedUnitRunsRegardlessOfBudget_AndIncrementsCount()
        {
            var battleState = new ReplayNotifier<BattleState>(BattleState.Running);
            double clockMs = 0.0;
            var scheduler = new EnemyAiScheduler(battleState, () => clockMs)
            {
                FrameBudgetMs = 0.5f
            };
            scheduler.Initialize();
            int starvedRuns = 0;

            scheduler.Register(TestPlayers.Enemy, 1f, 0f, () => clockMs += 1.0, isCritical: () => true);
            scheduler.Register(TestPlayers.SecondEnemy, 1f, 0f, () =>
            {
                starvedRuns++;
                clockMs += 0.1;
            });

            // Advance past 2 * interval so the second unit is starved even after the critical unit spends the budget.
            scheduler.Step(2.5f);
            Assert.That(starvedRuns, Is.EqualTo(1));
            Assert.That(scheduler.StarvationRunCount, Is.EqualTo(1));

            scheduler.LateDispose();
        }
    }
}
