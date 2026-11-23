using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Objectives;
using EmpireAtWar.Services.Enemy;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>Round 10 group rules: last stand latch (W48), scout first (W68), zone plans (W47); relations only.</summary>
    [Category("EnemyAi")]
    public sealed class EnemyGroupRulesTests
    {
        private const string ZONE = "zone:0";

        [Test]
        public void LastStand_NeedsAWeakFleetAndAThreatenedStation()
        {
            float weak = EnemyLastStandLatch.ADVANTAGE * 0.5f;
            float even = EnemyLastStandLatch.ADVANTAGE * 2f;

            Assert.That(new EnemyLastStandLatch().Update(weak, stationThreatened: false, now: 0f), Is.False,
                "A weak fleet with no station in danger keeps its tasks.");
            Assert.That(new EnemyLastStandLatch().Update(even, stationThreatened: true, now: 0f), Is.False,
                "A fleet near even strength answers the threat without a last stand.");
            Assert.That(new EnemyLastStandLatch().Update(float.NaN, stationThreatened: true, now: 0f), Is.False,
                "No rating yet: no last stand.");
            Assert.That(new EnemyLastStandLatch().Update(weak, stationThreatened: true, now: 0f), Is.True);
        }

        [Test]
        public void LastStand_HoldsAtLeastItsLatchTimeAfterTheLastTrigger()
        {
            float weak = EnemyLastStandLatch.ADVANTAGE * 0.5f;
            float even = EnemyLastStandLatch.ADVANTAGE * 2f;
            var latch = new EnemyLastStandLatch();
            latch.Update(weak, true, 0f);
            latch.Update(weak, true, 10f);

            Assert.That(latch.Update(even, false, 10f + EnemyLastStandLatch.SECONDS * 0.9f), Is.True,
                "Recovered strength does not end the last stand inside the latch time.");
            Assert.That(latch.Update(even, false, 10f + EnemyLastStandLatch.SECONDS), Is.False);
            Assert.That(latch.IsActive, Is.False);
        }

        [Test]
        public void LastStand_AllowsOnlyDefendGuardAndLocalFights()
        {
            foreach (EnemyStrategicState state in System.Enum.GetValues(typeof(EnemyStrategicState)))
            {
                bool allowed = state is EnemyStrategicState.DefendBase or EnemyStrategicState.GuardSite or
                    EnemyStrategicState.EngageLocal or EnemyStrategicState.RebuildFleet;
                Assert.That(EnemyShipTaskGroup.IsLastStandState(state), Is.EqualTo(allowed), state.ToString());
            }
            Assert.That(EnemyShipTaskGroup.IsLastStandState(EnemyStrategicState.CaptureZone), Is.False);
            Assert.That(EnemyShipTaskGroup.IsLastStandState(EnemyStrategicState.RaidStructures), Is.False);
        }

        [Test]
        public void StaleZone_GetsAScoutBeforeTheForceCommits()
        {
            var gate = new EnemyZoneScoutGate();

            Assert.That(gate.Step(ZONE, held: false, shipOnTheWay: false, now: 0f), Is.EqualTo(EnemyScoutStep.WaitForWing),
                "Unseen defenders: the force waits while a wing scouts.");
            Assert.That(gate.Step(ZONE, false, false, EnemyZoneScoutGate.WING_SECONDS),
                Is.EqualTo(EnemyScoutStep.ShipScouts), "No sighting after the wing head start: a ship scouts.");
            Assert.That(gate.Step(ZONE, false, shipOnTheWay: true, EnemyZoneScoutGate.WING_SECONDS + 1f),
                Is.EqualTo(EnemyScoutStep.WaitForWing), "One ship scout at a time; the force still waits.");

            gate.NoteSeen(ZONE, EnemyZoneScoutGate.WING_SECONDS + 2f);
            Assert.That(gate.Step(ZONE, false, true, EnemyZoneScoutGate.WING_SECONDS + 2f),
                Is.EqualTo(EnemyScoutStep.Commit), "Seen: the defenders are rated and the force goes.");
        }

        // W47 RaidAndLeave: once the zone is taken and clear, no ship stays.
        [Test]
        public void RaidAndLeave_KeepsNoShipOnceTheZoneIsTakenAndClear()
        {
            Assert.That(EnemyZoneTaskRules.LeavesZone(EnemyZonePlan.RaidAndLeave, Zone(owned: true)), Is.True);
            Assert.That(EnemyZoneTaskRules.LeavesZone(EnemyZonePlan.TakeAndHold, Zone(owned: true)), Is.False,
                "TakeAndHold keeps its garrison.");
            Assert.That(EnemyZoneTaskRules.LeavesZone(EnemyZonePlan.RaidAndLeave, Zone(owned: false)), Is.False,
                "The raid finishes the zone before it leaves.");
            Assert.That(EnemyZoneTaskRules.LeavesZone(EnemyZonePlan.RaidAndLeave,
                Zone(owned: true, staticDefenders: 1)), Is.False, "Hostile structures are destroyed first.");
        }

        // W47: a garrison stands at the zone centre; a HoldBackAndCounter force waits outside while the zone is quiet and
        // goes back in on a retake.
        [Test]
        public void ZoneGuardPoint_GarrisonAtTheCentre_HoldBackOutsideUntilARetake()
        {
            Vector3 centre = Vector3.zero;
            Vector3 holdBack = Vector3.right * 1500f;

            Assert.That(EnemyZoneTaskRules.GetGuardPoint(EnemyZonePlan.TakeAndHold, Zone(owned: true), centre, holdBack),
                Is.EqualTo(centre));
            Assert.That(EnemyZoneTaskRules.GetGuardPoint(EnemyZonePlan.HoldBackAndCounter, Zone(owned: true), centre,
                holdBack), Is.EqualTo(holdBack));
            Assert.That(EnemyZoneTaskRules.GetGuardPoint(EnemyZonePlan.HoldBackAndCounter,
                Zone(owned: true, hostileNear: true), centre, holdBack), Is.EqualTo(centre), "A retake is hit at once.");
            Assert.That(EnemyZoneTaskRules.HoldsBack(EnemyZonePlan.HoldBackAndCounter, Zone(owned: false)), Is.False,
                "A zone not owned is captured as usual.");
        }

        [Test]
        public void AllyCombined_WaitsUntilAlliedShipsCanReachTheZone()
        {
            Assert.That(EnemyZoneTaskRules.WaitsForAllies(EnemyZonePlan.AllyCombined, alliesCanReach: false), Is.True);
            Assert.That(EnemyZoneTaskRules.WaitsForAllies(EnemyZonePlan.AllyCombined, alliesCanReach: true), Is.False);
            Assert.That(EnemyZoneTaskRules.WaitsForAllies(EnemyZonePlan.TakeAndHold, alliesCanReach: false), Is.False);
        }

        private static EnemyZoneObjective Zone(bool owned, bool hostileNear = false, int staticDefenders = 0)
        {
            var zone = new EnemyZoneObjective();
            zone.Reset(ZONE, isCentre: false, isOwnSide: true, priority: 0f, travelSeconds: 0f,
                staticDefenders: staticDefenders, denyReason: null);
            zone.SetWorth(owned, 0f, 0f);
            zone.SetHold(0f, hostileNear, 0f, 1f);
            return zone;
        }

        [Test]
        public void ScoutWait_IsBoundedAndHeldOrFreshZonesCommitAtOnce()
        {
            var gate = new EnemyZoneScoutGate();
            gate.Step(ZONE, false, false, 0f);
            Assert.That(gate.Step(ZONE, false, true, EnemyZoneScoutGate.WAIT_SECONDS), Is.EqualTo(EnemyScoutStep.Commit),
                "A scout that finds nothing in time does not stall the attack.");

            Assert.That(new EnemyZoneScoutGate().Step(ZONE, held: true, false, 0f), Is.EqualTo(EnemyScoutStep.Commit));
            var seen = new EnemyZoneScoutGate();
            seen.NoteSeen(ZONE, 0f);
            Assert.That(seen.Step(ZONE, false, false, EnemyZoneScoutGate.FRESH_SECONDS), Is.EqualTo(EnemyScoutStep.Commit));
            Assert.That(seen.Step(ZONE, false, false, EnemyZoneScoutGate.FRESH_SECONDS + 1f),
                Is.EqualTo(EnemyScoutStep.WaitForWing), "Old intel is stale again.");
        }
    }
}
