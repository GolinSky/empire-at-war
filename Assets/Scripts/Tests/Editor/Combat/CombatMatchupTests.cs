using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Combat")]
    public sealed class CombatMatchupTests
    {
        [Test]
        public void EqualForces_AreAnEvenFight()
        {
            UnitCombatProfile frigate = Profile(ShipClass.Frigate, 1000f, 500f, (ShipClass.Frigate, 20f));

            Assert.That(CombatMatchup.Advantage(Force(frigate, frigate), Force(frigate, frigate)),
                Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void DoubleForce_HasTwiceTheAdvantage()
        {
            UnitCombatProfile frigate = Profile(ShipClass.Frigate, 1000f, 500f, (ShipClass.Frigate, 20f));

            Assert.That(CombatMatchup.Advantage(Force(frigate, frigate), Force(frigate)),
                Is.EqualTo(2f).Within(1e-4f));
        }

        [Test]
        public void EmptyForces_AreClamped()
        {
            UnitCombatProfile frigate = Profile(ShipClass.Frigate, 1000f, 0f, (ShipClass.Frigate, 20f));

            Assert.That(CombatMatchup.Advantage(Force(frigate), new ForceComposition()),
                Is.EqualTo(CombatMatchup.MAX_ADVANTAGE));
            Assert.That(CombatMatchup.Advantage(new ForceComposition(), Force(frigate)),
                Is.EqualTo(CombatMatchup.MIN_ADVANTAGE));
        }

        [Test]
        public void ClassAttackerCannotHurt_TakesMaximumTime()
        {
            UnitCombatProfile pointDefense = Profile(ShipClass.Corvette, 500f, 0f, (ShipClass.Bomber, 50f));
            UnitCombatProfile capital = Profile(ShipClass.Capital, 5000f, 0f, (ShipClass.Capital, 10f));

            Assert.That(CombatMatchup.TimeToDestroy(Force(pointDefense), Force(capital)),
                Is.EqualTo(CombatMatchup.MAX_TIME));
        }

        [Test]
        public void UnhurtableMinority_OnlyStretchesKillTime()
        {
            UnitCombatProfile antiShip = Profile(ShipClass.Bomber, 100f, 0f, (ShipClass.Capital, 10f));
            UnitCombatProfile capital = Profile(ShipClass.Capital, 900f, 0f, (ShipClass.Capital, 0f));
            UnitCombatProfile fighter = Profile(ShipClass.Fighter, 100f, 0f, (ShipClass.Capital, 0f));

            // 90 s for the capital, stretched by the fighter's 10% share of durability.
            Assert.That(CombatMatchup.TimeToDestroy(Force(antiShip), Force(capital, fighter)),
                Is.EqualTo(100f).Within(1e-3f));
        }

        [Test]
        public void ShieldsAbsorbFirst_ThenEveryWeaponHitsHull()
        {
            UnitCombatProfile attacker = Profile(ShipClass.Frigate, 100f, 0f, (ShipClass.Frigate, 10f));
            UnitCombatProfile target = Profile(ShipClass.Frigate, 100f, 50f, (ShipClass.Frigate, 0f));

            // 50 shields at 10/s, then 100 hull at 10/s.
            Assert.That(CombatMatchup.TimeToDestroy(Force(attacker), Force(target)), Is.EqualTo(15f).Within(1e-4f));
        }

        [Test]
        public void PiercingWeapons_IgnoreShields()
        {
            UnitCombatProfile torpedoes = new UnitCombatProfile(ShipClass.Bomber, 50f, 0f,
                Rates(), Rates(), Rates((ShipClass.Capital, 10f)), Array.Empty<UnitCombatProfile>());
            UnitCombatProfile capital = Profile(ShipClass.Capital, 100f, 10000f, (ShipClass.Capital, 0f));

            Assert.That(CombatMatchup.TimeToDestroy(Force(torpedoes), Force(capital)), Is.EqualTo(10f).Within(1e-4f));
        }

        [Test]
        public void BomberSwarm_OutmatchesCapitalFleetOfEqualCost()
        {
            UnitCombatProfile capital = Profile(ShipClass.Capital, 8000f, 4000f,
                (ShipClass.Capital, 60f), (ShipClass.Bomber, 2f));
            UnitCombatProfile bombers = Profile(ShipClass.Bomber, 300f, 100f, (ShipClass.Capital, 40f));

            ForceComposition capitals = Force(capital, capital);
            ForceComposition swarm = Force(bombers, bombers, bombers, bombers, bombers, bombers, bombers, bombers);

            Assert.That(CombatMatchup.Advantage(capitals, swarm), Is.LessThan(1f));
        }

        [Test]
        public void CounterProduction_BuildsAntiStrikecraftAgainstBomberSwarm()
        {
            UnitCombatProfile capital = Profile(ShipClass.Capital, 8000f, 4000f,
                (ShipClass.Capital, 60f), (ShipClass.Bomber, 2f));
            UnitCombatProfile corvette = Profile(ShipClass.Corvette, 800f, 400f,
                (ShipClass.Bomber, 40f), (ShipClass.Capital, 2f));
            UnitCombatProfile bombers = Profile(ShipClass.Bomber, 300f, 100f,
                (ShipClass.Capital, 40f), (ShipClass.Corvette, 5f));
            ForceComposition own = Force(capital, capital);
            ForceComposition hostile = Force(bombers, bombers, bombers, bombers, bombers, bombers);
            List<ProductionCandidate> candidates = new List<ProductionCandidate>
            {
                new ProductionCandidate(capital, 4000),
                new ProductionCandidate(corvette, 1500)
            };

            bool selected = new EnemyCounterProductionModel(new NeutralShipClassMatchups()).TrySelect(own, hostile, candidates, out int index);

            Assert.That(selected, Is.True);
            Assert.That(index, Is.EqualTo(1));
        }

        [Test]
        public void StrikecraftSpecialist_ComparesAverageStrikecraftAndWarshipDamage()
        {
            UnitCombatProfile corvette = Profile(ShipClass.Corvette, 800f, 400f,
                (ShipClass.Bomber, 40f), (ShipClass.Capital, 2f));
            UnitCombatProfile capital = Profile(ShipClass.Capital, 8000f, 4000f,
                (ShipClass.Capital, 60f), (ShipClass.Bomber, 2f));

            Assert.That(corvette.IsStrikecraftSpecialist(), Is.True);
            Assert.That(capital.IsStrikecraftSpecialist(), Is.False);
        }

        [TestCase(1, true)]
        [TestCase(0, false)]
        public void CounterProduction_SkipsStrikecraftSpecialistOnceScreensClearKnownStrikecraft(
            int existingScreens, bool expectedCovered)
        {
            UnitCombatProfile capital = Profile(ShipClass.Capital, 8000f, 4000f,
                (ShipClass.Capital, 60f), (ShipClass.Bomber, 2f));
            UnitCombatProfile corvette = Profile(ShipClass.Corvette, 800f, 400f,
                (ShipClass.Bomber, 40f), (ShipClass.Capital, 2f));
            UnitCombatProfile bombers = Profile(ShipClass.Bomber, 300f, 100f,
                (ShipClass.Capital, 40f), (ShipClass.Corvette, 5f));
            ForceComposition own = Force(capital, capital);
            for (int i = 0; i < existingScreens; i++) own.AddNew(corvette, false);
            var model = new EnemyCounterProductionModel(new NeutralShipClassMatchups());

            bool selected = model.TrySelect(own, Force(bombers, capital),
                new[] { new ProductionCandidate(corvette, 1500) }, out int index);

            // One screen kills the single known bomber in about 9 s; without it, two capitals need 100 s.
            Assert.That(model.StrikecraftCovered, Is.EqualTo(expectedCovered));
            Assert.That(selected, Is.EqualTo(!expectedCovered));
            Assert.That(index, Is.EqualTo(expectedCovered ? -1 : 0));
        }

        [Test]
        public void CounterProduction_BuildsCapitalsAgainstCapitals()
        {
            UnitCombatProfile capital = Profile(ShipClass.Capital, 8000f, 4000f,
                (ShipClass.Capital, 60f), (ShipClass.Bomber, 2f));
            UnitCombatProfile corvette = Profile(ShipClass.Corvette, 800f, 400f,
                (ShipClass.Bomber, 40f), (ShipClass.Capital, 2f));
            List<ProductionCandidate> candidates = new List<ProductionCandidate>
            {
                new ProductionCandidate(capital, 4000),
                new ProductionCandidate(corvette, 1500)
            };

            bool selected = new EnemyCounterProductionModel(new NeutralShipClassMatchups()).TrySelect(
                Force(capital), Force(capital, capital), candidates, out int index);

            Assert.That(selected, Is.True);
            Assert.That(index, Is.EqualTo(0));
        }

        [Test]
        public void CounterProduction_RepeatedTypeLosesToFreshType()
        {
            UnitCombatProfile capital = Profile(ShipClass.Capital, 8000f, 4000f, (ShipClass.Capital, 60f));
            UnitCombatProfile cruiser = Profile(ShipClass.Cruiser, 5000f, 2500f, (ShipClass.Capital, 30f));
            List<ProductionCandidate> candidates = new List<ProductionCandidate>
            {
                new ProductionCandidate(capital, 4000, ownedCount: 4),
                new ProductionCandidate(cruiser, 3000, ownedCount: 0)
            };

            bool selected = new EnemyCounterProductionModel(new NeutralShipClassMatchups()).TrySelect(
                Force(capital), Force(capital, capital), candidates, out int index);

            Assert.That(selected, Is.True);
            Assert.That(index, Is.EqualTo(1));
        }

        [Test]
        public void CounterProduction_StrongOwnedTypeStillBeatsAMuchWeakerFreshType()
        {
            UnitCombatProfile capital = Profile(ShipClass.Capital, 8000f, 4000f, (ShipClass.Capital, 60f));
            UnitCombatProfile weak = Profile(ShipClass.Capital, 3000f, 1000f, (ShipClass.Capital, 10f));
            List<ProductionCandidate> candidates = new List<ProductionCandidate>
            {
                new ProductionCandidate(capital, 4000, ownedCount: 1),
                new ProductionCandidate(weak, 2000, ownedCount: 0)
            };

            bool selected = new EnemyCounterProductionModel(new NeutralShipClassMatchups()).TrySelect(
                Force(capital), Force(capital, capital), candidates, out int index);

            Assert.That(selected, Is.True);
            Assert.That(index, Is.EqualTo(0));
        }

        [Test]
        public void CounterProduction_MatchupKnowledgeShiftsThePick()
        {
            UnitCombatProfile capital = Profile(ShipClass.Capital, 8000f, 4000f, (ShipClass.Capital, 60f));
            UnitCombatProfile cruiser = Profile(ShipClass.Cruiser, 5000f, 2500f, (ShipClass.Capital, 30f));
            List<ProductionCandidate> candidates = new List<ProductionCandidate>
            {
                new ProductionCandidate(capital, 4000),
                new ProductionCandidate(cruiser, 4000)
            };

            new EnemyCounterProductionModel(new NeutralShipClassMatchups()).TrySelect(
                Force(capital), Force(capital, capital), candidates, out int neutralPick);
            new EnemyCounterProductionModel(new FavorClass(ShipClass.Cruiser)).TrySelect(
                Force(capital), Force(capital, capital), candidates, out int favoredPick);

            Assert.That(neutralPick, Is.EqualTo(0));
            Assert.That(favoredPick, Is.EqualTo(1));
        }

        [Test]
        public void CounterProduction_CarrierWithoutSquadronRoomIsValuedWithoutHangar()
        {
            UnitCombatProfile interceptors = Profile(ShipClass.Interceptor, 400f, 100f, (ShipClass.Capital, 400f));
            UnitCombatProfile carrier = new UnitCombatProfile(ShipClass.Capital, 6000f, 3000f,
                Rates((ShipClass.Capital, 10f)), Rates((ShipClass.Capital, 10f)), Rates(),
                new[] { interceptors, interceptors, interceptors });
            UnitCombatProfile gunship = Profile(ShipClass.Capital, 6000f, 3000f, (ShipClass.Capital, 60f));

            int Pick(bool includeHangar)
            {
                new EnemyCounterProductionModel(new NeutralShipClassMatchups()).TrySelect(
                    Force(gunship), Force(gunship, gunship),
                    new[]
                    {
                        new ProductionCandidate(carrier, 4000, includeHangar: includeHangar),
                        new ProductionCandidate(gunship, 4000, includeHangar: includeHangar)
                    }, out int index);
                return index;
            }

            Assert.That(Pick(true), Is.EqualTo(0));
            Assert.That(Pick(false), Is.EqualTo(1));
        }

        [Test]
        public void CounterProduction_FallsBackWithoutHostiles()
        {
            UnitCombatProfile capital = Profile(ShipClass.Capital, 8000f, 4000f, (ShipClass.Capital, 60f));

            bool selected = new EnemyCounterProductionModel(new NeutralShipClassMatchups()).TrySelect(Force(capital), new ForceComposition(),
                new[] { new ProductionCandidate(capital, 4000) }, out int index);

            Assert.That(selected, Is.False);
            Assert.That(index, Is.EqualTo(-1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CounterProduction_CriticalStationAcceptsFastSquadron(bool hasKnownHostile)
        {
            UnitCombatProfile capital = Profile(ShipClass.Capital, 8000f, 4000f, (ShipClass.Capital, 60f));
            UnitCombatProfile bomber = Profile(ShipClass.Bomber, 1000f, 500f, (ShipClass.Capital, 40f));
            ProductionCandidate[] candidates =
            {
                new ProductionCandidate(capital, 4000, arrivalSeconds: 148f),
                new ProductionCandidate(bomber, 500, arrivalSeconds: 10f)
            };

            bool selected = new EnemyCounterProductionModel(new NeutralShipClassMatchups()).TrySelect(
                Force(capital), hasKnownHostile ? Force(capital, capital) : new ForceComposition(),
                candidates, out int index, isStationCritical: true, timeToLossSeconds: 100f);

            Assert.That(selected, Is.True);
            Assert.That(index, Is.EqualTo(1));
        }

        [TestCase(1000f, 120, 0)]
        [TestCase(100000f, 2, 1)]
        public void CounterProduction_PricesTheScarceResource(float credits, int capacity, int expected)
        {
            UnitCombatProfile capital = Profile(ShipClass.Capital, 8000f, 4000f, (ShipClass.Capital, 60f));
            ProductionCandidate[] candidates =
            {
                new ProductionCandidate(capital, 1000, unitCapacity: 8),
                new ProductionCandidate(capital, 4000, unitCapacity: 2)
            };

            bool selected = new EnemyCounterProductionModel(new NeutralShipClassMatchups()).TrySelect(
                Force(capital), Force(capital, capital), candidates, out int index, credits, capacity);

            Assert.That(selected, Is.True);
            Assert.That(index, Is.EqualTo(expected));
        }

        [Test]
        public void CarrierCandidate_CountsItsHangarSquadrons()
        {
            UnitCombatProfile interceptors = Profile(ShipClass.Interceptor, 400f, 100f, (ShipClass.Bomber, 30f));
            UnitCombatProfile carrier = new UnitCombatProfile(ShipClass.Capital, 6000f, 3000f,
                Rates((ShipClass.Capital, 30f)), Rates((ShipClass.Capital, 30f)), Rates(),
                new[] { interceptors, interceptors });
            ForceComposition force = new ForceComposition();

            force.AddNew(carrier, true);

            Assert.That(force.UnitCount, Is.EqualTo(3));
            Assert.That(force.HullDps(ShipClass.Bomber), Is.EqualTo(60f));
        }

        [TestCase(false, 0)]
        [TestCase(true, 1)]
        public void CounterProduction_CriticalStationChoosesTimelyReinforcement(bool critical, int expected)
        {
            UnitCombatProfile capital = Profile(ShipClass.Capital, 8000f, 4000f, (ShipClass.Capital, 60f));
            ProductionCandidate[] candidates =
            {
                new ProductionCandidate(capital, 1000, arrivalSeconds: 148f),
                new ProductionCandidate(capital, 4000, arrivalSeconds: 55f)
            };

            bool selected = new EnemyCounterProductionModel(new NeutralShipClassMatchups()).TrySelect(
                Force(capital), Force(capital, capital), candidates, out int index,
                isStationCritical: critical, timeToLossSeconds: 100f);

            Assert.That(selected, Is.True);
            Assert.That(index, Is.EqualTo(expected));
        }

        [TestCase(20f, false, -1)]
        [TestCase(50f, true, 1)]
        public void CounterProduction_CriticalStationUsesArrivalDeadlineAndPrice(
            float timeToLoss, bool expectedSelection, int expectedIndex)
        {
            UnitCombatProfile capital = Profile(ShipClass.Capital, 8000f, 4000f, (ShipClass.Capital, 60f));
            ProductionCandidate[] candidates =
            {
                new ProductionCandidate(capital, 4000, arrivalSeconds: 30f),
                new ProductionCandidate(capital, 1000, arrivalSeconds: 50f)
            };

            bool selected = new EnemyCounterProductionModel(new NeutralShipClassMatchups()).TrySelect(
                Force(capital), Force(capital, capital), candidates, out int index,
                isStationCritical: true, timeToLossSeconds: timeToLoss);

            Assert.That(selected, Is.EqualTo(expectedSelection));
            Assert.That(index, Is.EqualTo(expectedIndex));
        }

        private sealed class FavorClass : IShipClassMatchups
        {
            private readonly ShipClass _favored;

            public FavorClass(ShipClass favored) => _favored = favored;

            public float GetPreference(ShipClass candidate, ForceComposition hostile) =>
                candidate == _favored ? 3f : 1f;
        }

        // W15: shield regeneration in the strength math.
        [Test]
        public void ShieldRegenAtOrAboveShieldDamage_ShieldsAreNeverBroken()
        {
            Assert.That(CombatMatchup.TimeToDestroyClass(1000f, 500f, 50f, 50f, 0f, shieldRegen: 50f),
                Is.EqualTo(CombatMatchup.MAX_TIME), "Without piercing damage the unit cannot be destroyed.");
            Assert.That(CombatMatchup.TimeToDestroyClass(1000f, 500f, 50f, 50f, 10f, shieldRegen: 80f),
                Is.EqualTo(1000f / 10f).Within(1e-3f), "Only piercing damage counts.");
        }

        [Test]
        public void ShieldRegenBelowShieldDamage_SlowsTheKill()
        {
            float plain = CombatMatchup.TimeToDestroyClass(1000f, 500f, 50f, 50f, 0f);
            float regenerating = CombatMatchup.TimeToDestroyClass(1000f, 500f, 50f, 50f, 0f, shieldRegen: 25f);

            Assert.That(regenerating, Is.GreaterThan(plain));
            Assert.That(regenerating, Is.LessThan(CombatMatchup.MAX_TIME));
        }

        [Test]
        public void MoreShieldRegen_LowersTheAdvantage()
        {
            UnitCombatProfile frigate = Profile(ShipClass.Frigate, 1000f, 500f, (ShipClass.Frigate, 20f));
            ForceComposition own = Force(frigate, frigate);

            float none = CombatMatchup.Advantage(own, Force(Regenerating(frigate, 0f)));
            float some = CombatMatchup.Advantage(own, Force(Regenerating(frigate, 5f)));
            float more = CombatMatchup.Advantage(own, Force(Regenerating(frigate, 15f)));

            Assert.That(some, Is.LessThan(none));
            Assert.That(more, Is.LessThan(some));
        }

        [Test]
        public void ShieldRegen_IsRatedPerUnitOfTheClass()
        {
            UnitCombatProfile frigate = Regenerating(Profile(ShipClass.Frigate, 1000f, 500f), 6f);
            ForceComposition force = Force(frigate, frigate, frigate);

            Assert.That(force.ShieldRegenPerUnit(ShipClass.Frigate), Is.EqualTo(frigate.ShieldRegen).Within(1e-4f),
                "Focus fire meets one unit's regeneration at a time.");
        }

        [Test]
        public void LostShieldGenerator_AddsNoRegen()
        {
            UnitCombatProfile frigate = Regenerating(Profile(ShipClass.Frigate, 1000f, 500f), 6f);
            ForceComposition force = new ForceComposition();
            force.Add(frigate, frigate.Hull, 0f, 1f, 0f);

            Assert.That(force.ShieldRegenPerUnit(ShipClass.Frigate), Is.Zero);
        }

        // W16a: repair in the strength math.
        [Test]
        public void RepairSourceCoveringTheHostile_LowersTheAdvantage()
        {
            UnitCombatProfile frigate = Profile(ShipClass.Frigate, 1000f, 500f, (ShipClass.Frigate, 20f));
            ForceComposition own = Force(frigate, frigate);
            ForceComposition repaired = Force(frigate);
            repaired.AddRepairSource(0.005f, 2);

            Assert.That(CombatMatchup.Advantage(own, repaired), Is.LessThan(CombatMatchup.Advantage(own, Force(frigate))));
        }

        [Test]
        public void RepairSources_StackOnlyUpToTheHealersPerTarget()
        {
            ForceComposition force = Force(Profile(ShipClass.Frigate, 1000f, 500f));
            force.AddRepairSource(0.005f, 2);
            float one = force.RepairFractionPerSecond;
            force.AddRepairSource(0.005f, 2);
            float two = force.RepairFractionPerSecond;
            force.AddRepairSource(0.005f, 2);

            Assert.That(two, Is.EqualTo(2f * one).Within(1e-6f));
            Assert.That(force.RepairFractionPerSecond, Is.EqualTo(two).Within(1e-6f));
        }

        [Test]
        public void Repair_NeverHealsStrikecraft()
        {
            ForceComposition force = Force(Profile(ShipClass.Fighter, 100f, 0f), Profile(ShipClass.Frigate, 1000f, 500f));
            force.AddRepairSource(0.005f, 2);

            Assert.That(force.HullRepairPerUnit(ShipClass.Fighter), Is.Zero);
            Assert.That(force.HullRepairPerUnit(ShipClass.Frigate), Is.GreaterThan(0f));
        }

        // W17: a known hostile platform counts in a local fight.
        [Test]
        public void KnownHostilePlatform_LowersTheLocalAdvantage()
        {
            UnitCombatProfile frigate = Profile(ShipClass.Frigate, 1000f, 500f, (ShipClass.Frigate, 20f));
            UnitCombatProfile platform = Profile(ShipClass.Structure, 3000f, 1000f, (ShipClass.Frigate, 30f));
            ForceComposition own = Force(frigate, frigate);
            ForceComposition withPlatform = Force(frigate);
            withPlatform.AddNew(platform, false);

            Assert.That(CombatMatchup.Advantage(own, withPlatform), Is.LessThan(CombatMatchup.Advantage(own, Force(frigate))));
        }

        private static UnitCombatProfile Regenerating(UnitCombatProfile unit, float regen)
        {
            float[] hullDps = new float[ForceComposition.ClassSlots];
            float[] shieldDps = new float[ForceComposition.ClassSlots];
            float[] piercingDps = new float[ForceComposition.ClassSlots];
            foreach (ShipClass target in ForceComposition.Classes)
            {
                hullDps[(int)target] = unit.HullDps(target);
                shieldDps[(int)target] = unit.ShieldDps(target);
                piercingDps[(int)target] = unit.PiercingDps(target);
            }
            return new UnitCombatProfile(unit.ShipClass, unit.Hull, unit.Shields, hullDps, shieldDps, piercingDps,
                Array.Empty<UnitCombatProfile>(), regen);
        }

        private static ForceComposition Force(params UnitCombatProfile[] units)
        {
            ForceComposition force = new ForceComposition();
            foreach (UnitCombatProfile unit in units)
            {
                force.AddNew(unit, false);
            }

            return force;
        }

        /// <summary>A unit whose shield-blocked weapons deal the same damage to hull and shields.</summary>
        private static UnitCombatProfile Profile(
            ShipClass shipClass,
            float hull,
            float shields,
            params (ShipClass Target, float Dps)[] damage)
        {
            return new UnitCombatProfile(shipClass, hull, shields, Rates(damage), Rates(damage), Rates(),
                Array.Empty<UnitCombatProfile>());
        }

        private static float[] Rates(params (ShipClass Target, float Dps)[] damage)
        {
            float[] rates = new float[ForceComposition.ClassSlots];
            foreach ((ShipClass target, float dps) in damage)
            {
                rates[(int)target] = dps;
            }

            return rates;
        }
    }
}
