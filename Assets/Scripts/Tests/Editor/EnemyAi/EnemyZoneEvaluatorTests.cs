using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.EnemyFaction.Models.Objectives;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class EnemyZoneEvaluatorTests
    {
        private const float COMMIT = 0.8f;

        [Test]
        public void Assess_UndefendedZoneIsGo()
        {
            EnemyZoneAssessment result = new EnemyZoneEvaluator().Assess(Zone("a"), Force(1), COMMIT);

            Assert.That(result.Verdict, Is.EqualTo(EnemyZoneVerdict.Go));
        }

        [Test]
        public void Assess_StrongerDefendersAssembleAndRecordTheMissingForce()
        {
            EnemyZoneObjective zone = Zone("a");
            AddShips(zone.Defenders, 6);

            EnemyZoneAssessment result = new EnemyZoneEvaluator().Assess(zone, Force(2), COMMIT);

            Assert.That(result.Verdict, Is.EqualTo(EnemyZoneVerdict.Assemble));
            Assert.That(result.RequiredScale, Is.GreaterThan(1f));
        }

        [Test]
        public void Assess_ArmedStructureCountsAsDefender()
        {
            EnemyZoneObjective zone = Zone("a");
            zone.Defenders.AddNew(Profile(ShipClass.Structure, hull: 20000f, dps: 400f), false);

            EnemyZoneAssessment result = new EnemyZoneEvaluator().Assess(zone, Force(1), COMMIT);

            Assert.That(result.Verdict, Is.EqualTo(EnemyZoneVerdict.Assemble));
        }

        [Test]
        public void Assess_EnoughForceAgainstDefendersIsGo()
        {
            EnemyZoneObjective zone = Zone("a");
            AddShips(zone.Defenders, 1);

            EnemyZoneAssessment result = new EnemyZoneEvaluator().Assess(zone, Force(4), COMMIT);

            Assert.That(result.Verdict, Is.EqualTo(EnemyZoneVerdict.Go));
        }

        [Test]
        public void Select_DeniedZoneIsNeverChosen()
        {
            var zones = new List<EnemyZoneObjective> { Zone("base", priority: 5000f, denyReason: "hostileBase") };

            int selected = new EnemyZoneEvaluator().Select(zones, Force(10), COMMIT, false,
                new List<EnemyZoneAssessment>());

            Assert.That(selected, Is.EqualTo(-1));
        }

        [Test]
        public void Select_AlwaysReturnsATargetWhenEveryZoneNeedsMoreForce()
        {
            EnemyZoneObjective near = Zone("near", priority: 500f);
            EnemyZoneObjective far = Zone("far", priority: 100f);
            AddShips(near.Defenders, 8);
            AddShips(far.Defenders, 8);
            var results = new List<EnemyZoneAssessment>();

            int selected = new EnemyZoneEvaluator().Select(new List<EnemyZoneObjective> { far, near }, Force(1),
                COMMIT, false, results);

            Assert.That(selected, Is.EqualTo(1));
            Assert.That(results[selected].Verdict, Is.EqualTo(EnemyZoneVerdict.Assemble));
        }

        [Test]
        public void Select_GoZoneWinsOverAMoreValuableAssembleZone()
        {
            EnemyZoneObjective rich = Zone("rich", priority: 5000f);
            AddShips(rich.Defenders, 8);
            EnemyZoneObjective poor = Zone("poor", priority: 100f);

            int selected = new EnemyZoneEvaluator().Select(new List<EnemyZoneObjective> { rich, poor }, Force(1),
                COMMIT, false, new List<EnemyZoneAssessment>());

            Assert.That(selected, Is.EqualTo(1));
        }

        [Test]
        public void Select_BehindOnZones_OwnSideZoneComesBeforeTheEnemySide()
        {
            EnemyZoneObjective enemySide = Zone("enemySide", priority: 1000f);
            EnemyZoneObjective ownSide = Zone("ownSide", ownSide: true, priority: 100f);
            AddShips(ownSide.Defenders, 8);
            var zones = new List<EnemyZoneObjective> { enemySide, ownSide };
            var evaluator = new EnemyZoneEvaluator();

            Assert.That(evaluator.Select(zones, Force(1), COMMIT, true, new List<EnemyZoneAssessment>()),
                Is.EqualTo(1));
            Assert.That(evaluator.Select(zones, Force(1), COMMIT, false, new List<EnemyZoneAssessment>()),
                Is.EqualTo(0));
        }

        [Test]
        public void Select_BehindOnZones_DefendedCentreComesLastButAnUndefendedCentreIsTaken()
        {
            EnemyZoneObjective centre = Zone("centre", centre: true, priority: 5000f);
            EnemyZoneObjective outer = Zone("outer", priority: 100f);
            AddShips(outer.Defenders, 8);
            var evaluator = new EnemyZoneEvaluator();

            Assert.That(evaluator.Select(new List<EnemyZoneObjective> { centre, outer }, Force(1), COMMIT, true,
                new List<EnemyZoneAssessment>()), Is.EqualTo(0), "The enemy left the centre: take it.");

            AddShips(centre.Defenders, 8);
            Assert.That(evaluator.Select(new List<EnemyZoneObjective> { centre, outer }, Force(1), COMMIT, true,
                new List<EnemyZoneAssessment>()), Is.EqualTo(1));
            Assert.That(evaluator.Select(new List<EnemyZoneObjective> { centre }, Force(1), COMMIT, true,
                new List<EnemyZoneAssessment>()), Is.EqualTo(0), "The centre is never excluded.");
        }

        [Test]
        public void Assess_PricedForce_ZoneValueDecidesAnAttackOnUnpricedDefenders()
        {
            // Platforms cost nothing to lose: only the zone pays for the own losses of a won fight.
            EnemyZoneObjective worthless = Zone("worthless");
            EnemyZoneObjective valuable = Zone("valuable");
            AddShips(worthless.Defenders, 2);
            AddShips(valuable.Defenders, 2);
            ForceComposition force = PricedForce(3, price: 1000f);
            valuable.SetWorth(false, value: force.Value, worthShare: 0.5f);
            var evaluator = new EnemyZoneEvaluator();

            EnemyZoneAssessment noGain = evaluator.Assess(worthless, force, EnemyZoneEvaluator.COMMIT_ADVANTAGE);
            EnemyZoneAssessment gain = evaluator.Assess(valuable, force, EnemyZoneEvaluator.COMMIT_ADVANTAGE);

            Assert.That(noGain.Advantage, Is.GreaterThan(EnemyZoneEvaluator.COMMIT_ADVANTAGE),
                "The force wins the fight in both zones.");
            Assert.That(noGain.Verdict, Is.EqualTo(EnemyZoneVerdict.Assemble), "Winning alone gains nothing.");
            Assert.That(noGain.RequiredScale, Is.GreaterThan(1f));
            Assert.That(gain.Verdict, Is.EqualTo(EnemyZoneVerdict.Go));
            Assert.That(gain.RequiredScale, Is.LessThanOrEqualTo(1f));
        }

        [Test]
        public void Assess_PricedForce_BetterTradesMakeTheSameAttackWorthIt()
        {
            EnemyZoneObjective careful = Zone("careful");
            EnemyZoneObjective brave = Zone("brave");
            AddShips(careful.Defenders, 2);
            AddShips(brave.Defenders, 2);
            ForceComposition force = PricedForce(3, price: 1000f);
            (float ownLoss, _) = CombatMatchup.PredictLossShares(1.5f);
            // Worth a bit less than the losses of a fight at the rated advantage.
            float value = 0.8f * ownLoss * force.Value;
            careful.SetWorth(false, value, 0.5f);
            brave.SetWorth(false, value, 0.5f);
            brave.SetHold(0f, false, 0f, tradeCorrection: 2f);
            var evaluator = new EnemyZoneEvaluator();

            Assert.That(evaluator.Assess(careful, force, EnemyZoneEvaluator.COMMIT_ADVANTAGE).Verdict,
                Is.EqualTo(EnemyZoneVerdict.Assemble));
            Assert.That(evaluator.Assess(brave, force, EnemyZoneEvaluator.COMMIT_ADVANTAGE).Verdict,
                Is.EqualTo(EnemyZoneVerdict.Go), "After better trades than rated the AI expects fewer losses.");
        }

        private static ForceComposition PricedForce(int ships, float price)
        {
            float[] rates = new float[ForceComposition.ClassSlots];
            for (int i = 0; i < rates.Length; i++) rates[i] = 50f;
            var profile = new UnitCombatProfile(ShipClass.Frigate, 1000f, 0f, rates, rates,
                new float[ForceComposition.ClassSlots], Array.Empty<UnitCombatProfile>(), price: price);
            var force = new ForceComposition();
            for (int i = 0; i < ships; i++) force.AddNew(profile, false);
            return force;
        }

        private static EnemyZoneObjective Zone(string id, bool centre = false, bool ownSide = false,
            float priority = 500f, string denyReason = null)
        {
            var zone = new EnemyZoneObjective();
            zone.Reset(id, centre, ownSide, priority, 30f, 0, denyReason);
            return zone;
        }

        private static ForceComposition Force(int ships)
        {
            var force = new ForceComposition();
            AddShips(force, ships);
            return force;
        }

        private static void AddShips(ForceComposition force, int ships)
        {
            for (int i = 0; i < ships; i++) force.AddNew(Profile(ShipClass.Frigate, hull: 1000f, dps: 50f), false);
        }

        private static UnitCombatProfile Profile(ShipClass shipClass, float hull, float dps)
        {
            float[] rates = new float[ForceComposition.ClassSlots];
            for (int i = 0; i < rates.Length; i++) rates[i] = dps;
            return new UnitCombatProfile(shipClass, hull, 0f, rates, rates,
                new float[ForceComposition.ClassSlots], Array.Empty<UnitCombatProfile>());
        }
    }
}
