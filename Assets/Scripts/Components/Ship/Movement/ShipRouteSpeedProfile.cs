using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Movement
{
    public sealed class ShipRouteSpeedProfile
    {
        public const float SPEED_RAMP_TIME = 1.5f;
        private readonly List<SpeedPhase> _phases = new List<SpeedPhase>();
        private readonly float[] _speedLimits;
        private readonly float[] _pieceStartTimes;
        private readonly float _startDistance;
        private readonly float _initialSpeed;
        private readonly float _acceleration;
        private readonly bool _paused;
        private float _distance;
        private float _speed;

        public float Duration { get; private set; }
        public float Acceleration => _acceleration;

        public ShipRouteSpeedProfile(ShipRoute route, float maxSpeed, float rotationSpeed,
            float initialSpeed, float rampTime = SPEED_RAMP_TIME, float startDistance = 0f)
        {
            _startDistance = Mathf.Clamp(startDistance, 0f, route.Length);
            _distance = _startDistance;
            _initialSpeed = Mathf.Max(0f, initialSpeed);
            _speed = _initialSpeed;
            _paused = maxSpeed <= 0f;
            _acceleration = (_paused ? _initialSpeed : maxSpeed) / rampTime;
            int count = route.Pieces.Count;
            _speedLimits = new float[count];
            _pieceStartTimes = new float[count];
            float[] boundary = new float[count + 1];
            int first = 0;
            for (int i = 0; i < count; i++)
            {
                float curvature = Mathf.Abs(route.Pieces[i].Curvature);
                _speedLimits[i] = curvature == 0f ? maxSpeed :
                    Mathf.Min(maxSpeed, rotationSpeed * Mathf.Deg2Rad / curvature);
                boundary[i] = i == 0 ? _speedLimits[i] : Mathf.Min(_speedLimits[i - 1], _speedLimits[i]);
                if (route.RequiresTurnAtPiece(i)) boundary[i] = 0f;
                if (route.GetPieceStartDistance(i) <= _startDistance) first = i;
            }

            if (_paused)
            {
                float stopAt = route.Length;
                for (int i = first + 1; i < count; i++)
                {
                    if (!route.RequiresTurnAtPiece(i)) continue;
                    stopAt = route.GetPieceStartDistance(i);
                    break;
                }
                if (_initialSpeed > 0f)
                    AddPhase(Mathf.Min(stopAt - _startDistance,
                        _initialSpeed * _initialSpeed / (2f * _acceleration)), 0f);
                // Ion disables movement without completing or discarding the order.
                // If the endpoint is closer than stopping distance, stop at that endpoint.
                Duration = float.PositiveInfinity;
                for (int i = 0; i < count; i++) _pieceStartTimes[i] = float.PositiveInfinity;
                return;
            }

            // Backward braking envelope, followed by forward acceleration feasibility.
            for (int i = count - 1; i >= first; i--)
            {
                float length = PieceLength(route, i);
                boundary[i] = Mathf.Min(boundary[i], ReachableSpeed(boundary[i + 1], length));
            }
            boundary[first] = Mathf.Min(boundary[first], _initialSpeed);
            for (int i = first; i < count; i++)
                boundary[i + 1] = Mathf.Min(boundary[i + 1], ReachableSpeed(boundary[i], PieceLength(route, i)));

            for (int i = first; i < count; i++)
            {
                _pieceStartTimes[i] = Duration;
                AddPiece(PieceLength(route, i), _speedLimits[i], boundary[i + 1]);
            }
        }

        public float GetSpeedLimit(int pieceIndex) => _speedLimits[pieceIndex];
        public float GetPieceStartTime(int pieceIndex) => _pieceStartTimes[pieceIndex];

        public void EvaluateTime(float time, ref int phaseIndex, out float distance, out float speed)
        {
            if (_phases.Count == 0)
            {
                distance = _startDistance;
                speed = _paused ? 0f : _initialSpeed;
                return;
            }
            float clamped = Mathf.Clamp(time, 0f, Duration);
            while (phaseIndex < _phases.Count - 1 && clamped >= _phases[phaseIndex + 1].StartTime) phaseIndex++;
            while (phaseIndex > 0 && clamped < _phases[phaseIndex].StartTime) phaseIndex--;
            SpeedPhase phase = _phases[phaseIndex];
            float elapsed = Mathf.Clamp(clamped - phase.StartTime, 0f, phase.Duration);
            speed = Mathf.Max(0f, phase.Speed + phase.Acceleration * elapsed);
            distance = phase.Distance + phase.Speed * elapsed + 0.5f * phase.Acceleration * elapsed * elapsed;
            if (clamped >= Duration)
            {
                distance = _distance;
                speed = _speed;
            }
        }

        private float PieceLength(ShipRoute route, int index) => Mathf.Max(0f,
            route.GetPieceStartDistance(index) + route.Pieces[index].Length - Mathf.Max(_startDistance, route.GetPieceStartDistance(index)));

        private float ReachableSpeed(float speed, float length) => Mathf.Sqrt(speed * speed + 2f * _acceleration * length);

        private void AddPiece(float length, float limit, float endSpeed)
        {
            if (length <= 0f) return;
            // Replans can begin above the feasible envelope. Preserve velocity and brake;
            // carry the residual speed into the next piece instead of snapping to its cap.
            if (_speed * _speed > endSpeed * endSpeed + 2f * _acceleration * length)
            {
                float exit = Mathf.Sqrt(Mathf.Max(0f, _speed * _speed - 2f * _acceleration * length));
                AddPhase(length, exit);
                return;
            }
            if (_speed > limit)
            {
                float brakingLength = (_speed * _speed - limit * limit) / (2f * _acceleration);
                AddPhase(brakingLength, limit);
                length -= brakingLength;
            }
            float peak = Mathf.Min(limit,
                Mathf.Sqrt(_acceleration * length + 0.5f * (_speed * _speed + endSpeed * endSpeed)));
            float accelerationLength = Mathf.Max(0f, (peak * peak - _speed * _speed) / (2f * _acceleration));
            float brakingDistance = Mathf.Max(0f, (peak * peak - endSpeed * endSpeed) / (2f * _acceleration));
            AddPhase(accelerationLength, peak);
            AddPhase(Mathf.Max(0f, length - accelerationLength - brakingDistance), peak);
            AddPhase(brakingDistance, endSpeed);
        }

        private void AddPhase(float length, float endSpeed)
        {
            if (length <= 0f) return;
            float duration = 2f * length / (_speed + endSpeed);
            _phases.Add(new SpeedPhase(Duration, duration, _distance, _speed, (endSpeed - _speed) / duration));
            Duration += duration;
            _distance += length;
            _speed = endSpeed;
        }

        private readonly struct SpeedPhase
        {
            public readonly float StartTime;
            public readonly float Duration;
            public readonly float Distance;
            public readonly float Speed;
            public readonly float Acceleration;

            public SpeedPhase(float startTime, float duration, float distance, float speed, float acceleration)
            {
                StartTime = startTime;
                Duration = duration;
                Distance = distance;
                Speed = speed;
                Acceleration = acceleration;
            }
        }
    }
}
