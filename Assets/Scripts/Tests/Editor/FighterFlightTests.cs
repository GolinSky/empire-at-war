using System;
using System.Numerics;
using EmpireAtWar.Components.Squadrons.Flight;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class FighterFlightTests
    {
        private const float DELTA_TIME = 0.1f;
        private const float TURN_RATE = 90f;

        private readonly TestFlightData _data = new TestFlightData();

        [Test]
        public void Step_LimitsTurnRate()
        {
            FighterKinematics fighter = new FighterKinematics(Vector3.Zero, Vector3.UnitZ, 10f);

            fighter.Step(new Vector3(100f, 0f, 0f), 10f, _data, DELTA_TIME);

            float turned = FighterKinematics.GetYaw(fighter.Forward);
            Assert.That(turned, Is.EqualTo(TURN_RATE * DELTA_TIME).Within(0.01f));
        }

        [Test]
        public void Step_RollsRightWingDownWhenTurningRight()
        {
            FighterKinematics fighter = new FighterKinematics(Vector3.Zero, Vector3.UnitZ, 10f);

            fighter.Step(new Vector3(100f, 0f, 0f), 10f, _data, DELTA_TIME);

            Assert.That(fighter.Bank, Is.LessThan(0f));
        }

        [Test]
        public void Step_AcceleratesTowardsDesiredSpeed()
        {
            FighterKinematics fighter = new FighterKinematics(Vector3.Zero, Vector3.UnitZ, 10f);

            fighter.Step(new Vector3(0f, 0f, 100f), 20f, _data, DELTA_TIME);

            Assert.That(fighter.Speed, Is.EqualTo(10f + _data.Acceleration * DELTA_TIME).Within(0.001f));
            Assert.That(fighter.Position.Z, Is.GreaterThan(0f));
        }

        [Test]
        public void Step_NeverStops()
        {
            FighterKinematics fighter = new FighterKinematics(Vector3.Zero, Vector3.UnitZ, 10f);

            fighter.Step(Vector3.Zero, 10f, _data, DELTA_TIME);

            Assert.That(fighter.Position.Z, Is.GreaterThan(0f));
        }

        [Test]
        public void Maneuver_BreaksPastTargetAndReturnsForAnotherPass()
        {
            FighterManeuver maneuver = new FighterManeuver(7);
            maneuver.Reset(_data.FormationSpacing);
            Vector3 target = Vector3.Zero;

            maneuver.Resolve(new Vector3(0f, 0f, -_data.BreakDistance * 0.5f), Vector3.UnitZ, target, 0f, _data,
                out _);
            Assert.That(maneuver.Phase, Is.EqualTo(FighterManeuverPhase.Extend));

            maneuver.Resolve(new Vector3(0f, 0f, _data.ExtendDistance + 1f), Vector3.UnitZ, target, 0f, _data,
                out _);
            Assert.That(maneuver.Phase, Is.EqualTo(FighterManeuverPhase.Approach));
        }

        [Test]
        public void Formation_RotatesSlotsWithHeading()
        {
            Vector3 slot = SquadronFormation.GetSlot(1, 2f);
            Vector3 rotated = SquadronFormation.ToWorld(slot, Vector3.UnitX);

            Assert.That(slot.X, Is.LessThan(0f));
            Assert.That(Math.Abs(rotated.Z - -slot.X), Is.LessThan(0.001f));
        }

        private sealed class TestFlightData : IFighterFlightData
        {
            public float CruiseSpeed => 10f;
            public float CombatSpeed => 12f;
            public float Acceleration => 5f;
            public float TurnRate => TURN_RATE;
            public float MaxBankAngle => 60f;
            public float BankResponse => 5f;
            public float Height => 10f;
            public float FormationSpacing => 2f;
            public float LoiterRadius => 15f;
            public float BreakDistance => 5f;
            public float ExtendDistance => 30f;
        }
    }
}
