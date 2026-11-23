using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Utils;
using NUnit.Framework;
using UnityEngine;
using NumericsVector3 = System.Numerics.Vector3;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>Online client squadrons: local fighters stay on the host's replicated squadron root.</summary>
    [Category("Squadrons")]
    public sealed class SquadronReplicaAnchorTests
    {
        private const float DELTA_TIME = 0.02f;
        private const int FIGHTERS = 4;

        private readonly TestFlightData _data = new TestFlightData();

        [Test]
        public void FightersFarFromTheHostRoot_JumpThere_ThenOrbitItWithinTheLoiterRadius()
        {
            ModelFlight flight = new ModelFlight(_data, FIGHTERS, Vector3.zero);
            SquadronPilot pilot = new SquadronPilot(flight);
            pilot.Loiter(Vector3.zero);
            SquadronReplicaAnchor anchor = new SquadronReplicaAnchor(flight, pilot);
            Vector3 hostRoot = new Vector3(1000f, 0f, 0f);

            Fly(flight, pilot, anchor, hostRoot);
            Assert.That(Drift(flight, hostRoot), Is.LessThan(_data.LoiterRadius), "A jump moves the fighters at once.");

            float maxDrift = 0f;
            for (float time = 0f; time < 10f; time += DELTA_TIME)
            {
                Fly(flight, pilot, anchor, hostRoot);
                maxDrift = Mathf.Max(maxDrift, Drift(flight, hostRoot));
            }

            Assert.That(pilot.HasArrived, Is.True, "A resting host squadron is orbited.");
            Assert.That(maxDrift, Is.LessThanOrEqualTo(_data.LoiterRadius * 1.25f));
        }

        [Test]
        public void MovingHostRoot_IsFollowedInAWedge_AndTheFightersKeepUp()
        {
            ModelFlight flight = new ModelFlight(_data, FIGHTERS, Vector3.zero);
            SquadronPilot pilot = new SquadronPilot(flight);
            pilot.Loiter(Vector3.zero);
            SquadronReplicaAnchor anchor = new SquadronReplicaAnchor(flight, pilot);
            Vector3 velocity = Vector3.right * (_data.CruiseSpeed * 0.8f);
            Vector3 hostRoot = new Vector3(0f, 0f, 30f);

            float maxDrift = 0f;
            for (float time = 0f; time < 15f; time += DELTA_TIME)
            {
                hostRoot += velocity * DELTA_TIME;
                Fly(flight, pilot, anchor, hostRoot);
                // Settling: the wedge forms and catches up first.
                if (time > 3f) maxDrift = Mathf.Max(maxDrift, Drift(flight, hostRoot));
            }

            Assert.That(pilot.IsFollowing, Is.True);
            Assert.That(maxDrift, Is.LessThanOrEqualTo(_data.LoiterRadius),
                "The fighters fly with the host's squadron instead of loitering where they were (D1).");
        }

        [Test]
        public void Drift_BeyondTheLoiterRadius_IsPulledBackWithoutAJump()
        {
            ModelFlight flight = new ModelFlight(_data, FIGHTERS, Vector3.zero);
            SquadronPilot pilot = new SquadronPilot(flight);
            pilot.Loiter(Vector3.zero);
            SquadronReplicaAnchor anchor = new SquadronReplicaAnchor(flight, pilot);
            Vector3 hostRoot = new Vector3(_data.LoiterRadius * 3f, 0f, 0f);
            Vector3 before = flight.FighterCentroid;

            flight.Centroid = hostRoot;
            anchor.Steer(hostRoot, DELTA_TIME);

            float moved = Planar(flight.FighterCentroid - before);
            Assert.That(moved, Is.GreaterThan(0f), "Drift past the loiter radius is corrected.");
            Assert.That(moved, Is.LessThanOrEqualTo(_data.CruiseSpeed * DELTA_TIME),
                "Small drift is pulled back gradually, never faster than the fighters fly.");
        }

        private static void Fly(ModelFlight flight, SquadronPilot pilot, SquadronReplicaAnchor anchor, Vector3 hostRoot)
        {
            // ClientReplicationReceiver writes the host pose to the root before the squadron's next tick.
            flight.Centroid = hostRoot;
            anchor.Steer(hostRoot, DELTA_TIME);
            pilot.Tick(DELTA_TIME);
            flight.Step(DELTA_TIME);
        }

        private static float Drift(ModelFlight flight, Vector3 hostRoot) => Planar(flight.FighterCentroid - hostRoot);

        private static float Planar(Vector3 offset) => new Vector2(offset.x, offset.z).magnitude;

        /// <summary>The flight component's model without its views: the root is set by the test like the receiver does.</summary>
        private sealed class ModelFlight : ISquadronFlightComponent
        {
            private readonly SquadronFlightModel _model;

            public ModelFlight(IFighterFlightData data, int count, Vector3 origin)
            {
                _model = new SquadronFlightModel(data, new CombatModifiers());
                List<NumericsVector3> positions = new List<NumericsVector3>();
                for (int i = 0; i < count; i++)
                    positions.Add(origin.ToNumerics() + SquadronFormation.GetSlot(i, data.FormationSpacing));
                _model.Spawn(positions, NumericsVector3.UnitZ);
                Centroid = origin;
            }

            public string Id => "flight";
            public IFighterFlightData Data => _model.Data;
            public int Count => _model.Count;
            public Vector3 Centroid { get; set; }
            public Vector3 Heading => _model.GetHeading().ToUnity();
            public Vector3 FighterCentroid => _model.GetCentroid().ToUnity();
            public bool IsAlive(int index) => _model.IsAlive(index);
            public Vector3 GetPosition(int index) => _model.Get(index).Position.ToUnity();
            public Vector3 GetForward(int index) => _model.Get(index).Forward.ToUnity();
            public void Steer(int index, Vector3 target, float speed) => _model.SetSteering(index, target.ToNumerics(), speed);
            public void Step(float deltaTime) => _model.Step(deltaTime);
            public void Shift(Vector3 offset) => _model.Shift(offset.ToNumerics());
            public void SetSpeedCap(float cap) => _model.SetSpeedCap(cap);
            public void Respawn(Vector3 position, Quaternion rotation) => throw new NotSupportedException();
        }

        private sealed class TestFlightData : IFighterFlightData
        {
            public float CruiseSpeed => 40f;
            public float CombatSpeed => 45f;
            public float Acceleration => 40f;
            public float TurnRate => 180f;
            public float MaxBankAngle => 50f;
            public float BankResponse => 4f;
            public float Height => 10f;
            public float FormationSpacing => 3f;
            public float LoiterRadius => 20f;
            public float BreakDistance => 10f;
            public float ExtendDistance => 44f;
        }
    }
}
