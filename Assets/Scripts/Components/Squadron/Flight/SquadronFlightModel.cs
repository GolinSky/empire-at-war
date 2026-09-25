using System;
using System.Collections.Generic;
using System.Numerics;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Components.Squadrons.Flight
{
    public sealed class SquadronFlightModel : PureModel
    {
        private readonly IFighterFlightData _data;
        private readonly CombatModifiers _modifiers;
        private FighterKinematics[] _fighters = Array.Empty<FighterKinematics>();
        private bool[] _alive = Array.Empty<bool>();
        private Vector3[] _targets = Array.Empty<Vector3>();
        private float[] _speeds = Array.Empty<float>();

        public IFighterFlightData Data => _data;
        public int Count => _fighters.Length;

        public SquadronFlightModel(IFighterFlightData data, CombatModifiers modifiers)
        {
            _data = data;
            _modifiers = modifiers;
        }

        public void Spawn(IReadOnlyList<Vector3> positions, Vector3 forward)
        {
            int count = positions.Count;
            _fighters = new FighterKinematics[count];
            _alive = new bool[count];
            _targets = new Vector3[count];
            _speeds = new float[count];
            for (int i = 0; i < count; i++)
            {
                _fighters[i] = new FighterKinematics(positions[i], forward, _data.CruiseSpeed);
                _alive[i] = true;
                _targets[i] = positions[i] + forward * _data.LoiterRadius;
                _speeds[i] = _data.CruiseSpeed;
            }
        }

        public FighterKinematics Get(int index) => _fighters[index];
        public bool IsAlive(int index) => _alive[index];
        public void Kill(int index) => _alive[index] = false;

        public void SetSteering(int index, Vector3 target, float speed)
        {
            _targets[index] = target;
            _speeds[index] = speed;
        }

        public void Step(float deltaTime)
        {
            float speedMultiplier = _modifiers.SpeedMultiplier;
            for (int i = 0; i < _fighters.Length; i++)
            {
                if (_alive[i])
                {
                    _fighters[i].Step(_targets[i], _speeds[i] * speedMultiplier, _data, deltaTime);
                }
            }
        }

        public Vector3 GetCentroid()
        {
            Vector3 sum = Vector3.Zero;
            int alive = 0;
            for (int i = 0; i < _fighters.Length; i++)
            {
                if (!_alive[i]) continue;
                sum += _fighters[i].Position;
                alive++;
            }

            return alive == 0 ? GetLastPosition() : sum / alive;
        }

        public Vector3 GetHeading()
        {
            Vector3 sum = Vector3.Zero;
            for (int i = 0; i < _fighters.Length; i++)
            {
                if (_alive[i]) sum += _fighters[i].Forward;
            }

            sum.Y = 0f;
            return sum.LengthSquared() > 1e-6f ? Vector3.Normalize(sum) : Vector3.UnitZ;
        }

        private Vector3 GetLastPosition() => _fighters.Length == 0 ? Vector3.Zero : _fighters[0].Position;
    }
}
