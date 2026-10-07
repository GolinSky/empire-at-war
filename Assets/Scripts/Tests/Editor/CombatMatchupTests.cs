using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
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

            bool selected = new EnemyCounterProductionModel().TrySelect(own, hostile, candidates, out int index);

            Assert.That(selected, Is.True);
            Assert.That(index, Is.EqualTo(1));
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

            bool selected = new EnemyCounterProductionModel().TrySelect(
                Force(capital), Force(capital, capital), candidates, out int index);

            Assert.That(selected, Is.True);
            Assert.That(index, Is.EqualTo(0));
        }

        [Test]
        public void CounterProduction_FallsBackWithoutHostiles()
        {
            UnitCombatProfile capital = Profile(ShipClass.Capital, 8000f, 4000f, (ShipClass.Capital, 60f));

            bool selected = new EnemyCounterProductionModel().TrySelect(Force(capital), new ForceComposition(),
                new[] { new ProductionCandidate(capital, 4000) }, out int index);

            Assert.That(selected, Is.False);
            Assert.That(index, Is.EqualTo(-1));
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
