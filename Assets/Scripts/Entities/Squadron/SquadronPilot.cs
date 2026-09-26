using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Utils;
using UnityEngine;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Entities.Squadrons
{
    /// <summary>
    /// Turns the squadron's current intent into a steering point per fighter:
    /// travel in a loose wedge, orbit a point or escorted unit, or fly attack passes against a target.
    /// </summary>
    public sealed class SquadronPilot
    {
        private const float ARRIVAL_DISTANCE = 4f;
        private const float LEADER_TURN_SHARE = 0.6f;
        private const float LEADER_WAIT_SPEED_SHARE = 0.6f;
        private const float LEADER_WAIT_SPACINGS = 3f;
        private const float SLOT_LOOKAHEAD = 6f;
        private const float CATCH_UP_GAIN = 0.08f;
        private const float MIN_CATCH_UP_SPEED = 0.7f;
        private const float MAX_CATCH_UP_SPEED = 1.4f;
        private const float ORBIT_LEAD_ANGLE = 0.35f;
        private const float ORBIT_TRAIL_SPACINGS = 1.5f;
        private const float WOBBLE_SHARE = 0.25f;
        private const float AIM_REFRESH_INTERVAL = 0.5f;
        private const float MIN_TARGET_RADIUS = 2f;
        private const float APPROACH_RADIUS_SHARE = 0.5f;

        private enum Mode
        {
            Travel,
            Loiter,
            Engage,
        }

        private readonly ISquadronFlightComponent _flight;
        private readonly FighterManeuver[] _maneuvers;
        private Mode _mode = Mode.Loiter;
        private float _time;

        private Vector3 _destination;
        private Vector3 _leaderPosition;
        private Vector3 _leaderHeading = Vector3.forward;

        private Transform _escortAnchor;
        private Vector3 _loiterCenter;
        private float _loiterRadius;
        private float _orbitAngle;
        private float _orbitDirection = 1f;

        private IEntity _target;
        private float _targetRadius;
        private IHardPointModel[] _aimUnits = Array.Empty<IHardPointModel>();
        private float _aimRefreshTimer;

        public bool HasArrived => _mode == Mode.Loiter;
        public Vector3 LoiterCenter => _loiterCenter;
        private IFighterFlightData Data => _flight.Data;

        public SquadronPilot(ISquadronFlightComponent flight)
        {
            _flight = flight;
            _maneuvers = new FighterManeuver[flight.Count];
            for (int i = 0; i < _maneuvers.Length; i++)
            {
                _maneuvers[i] = new FighterManeuver(UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            }
        }

        public void FlyTo(Vector3 destination)
        {
            destination.y = Data.Height;
            _destination = destination;
            if (_mode != Mode.Travel)
            {
                _leaderPosition = _flight.Centroid;
                _leaderHeading = _flight.Heading;
            }

            _mode = Mode.Travel;
            _escortAnchor = null;
            _target = null;
        }

        public void Loiter(Vector3 center)
        {
            _escortAnchor = null;
            BeginLoiter(center, Data.LoiterRadius);
        }

        public void Escort(Transform anchor, float anchorRadius)
        {
            _escortAnchor = anchor;
            BeginLoiter(anchor.position, anchorRadius + Data.LoiterRadius);
        }

        public void Engage(IEntity target)
        {
            if (_mode == Mode.Engage && ReferenceEquals(_target, target))
            {
                return;
            }

            _mode = Mode.Engage;
            _target = target;
            _escortAnchor = null;
            _targetRadius = GetRadius(target);
            _aimUnits = target.HealthModel.GetShipUnits(HardPointType.Any);
            _aimRefreshTimer = AIM_REFRESH_INTERVAL;
            foreach (FighterManeuver maneuver in _maneuvers)
            {
                maneuver.Reset(Data.FormationSpacing + _targetRadius * APPROACH_RADIUS_SHARE);
            }
        }

        public void Tick(float deltaTime)
        {
            _time += deltaTime;
            switch (_mode)
            {
                case Mode.Travel:
                    TickTravel(deltaTime);
                    break;
                case Mode.Loiter:
                    TickLoiter(deltaTime);
                    break;
                case Mode.Engage:
                    TickEngage(deltaTime);
                    break;
            }
        }

        /// <summary>Largest distance from the target's centre to any of its hardpoints.</summary>
        public static float GetRadius(IEntity entity)
        {
            Vector3 center = entity.GetFacade<IEntityTransformFacade>().Transform.position;
            float radius = MIN_TARGET_RADIUS;
            foreach (IHardPointModel unit in entity.HealthModel.GetShipUnits(HardPointType.Any))
            {
                radius = Mathf.Max(radius, Vector3.Distance(center, unit.Position));
            }

            return radius;
        }

        private void BeginLoiter(Vector3 center, float radius)
        {
            _mode = Mode.Loiter;
            _target = null;
            _loiterCenter = center;
            _loiterRadius = radius;
            Vector3 offset = _flight.Centroid - center;
            _orbitAngle = Mathf.Atan2(offset.z, offset.x);
            Vector3 counterClockwiseTangent = new Vector3(-Mathf.Sin(_orbitAngle), 0f, Mathf.Cos(_orbitAngle));
            _orbitDirection = Vector3.Dot(_flight.Heading, counterClockwiseTangent) >= 0f ? 1f : -1f;
        }

        private void TickTravel(float deltaTime)
        {
            Vector3 toDestination = _destination - _leaderPosition;
            toDestination.y = 0f;
            float distance = toDestination.magnitude;
            if (distance <= ARRIVAL_DISTANCE)
            {
                BeginLoiter(_destination, Data.LoiterRadius);
                TickLoiter(deltaTime);
                return;
            }

            _leaderHeading = Vector3.RotateTowards(_leaderHeading, toDestination / distance,
                Data.TurnRate * LEADER_TURN_SHARE * Mathf.Deg2Rad * deltaTime, 0f);
            float leaderSpeed = GetSlotLag() > Data.FormationSpacing * LEADER_WAIT_SPACINGS
                ? Data.CruiseSpeed * LEADER_WAIT_SPEED_SHARE
                : Data.CruiseSpeed;
            _leaderPosition += _leaderHeading * Mathf.Min(leaderSpeed * deltaTime, distance);
            _leaderPosition.y = Data.Height;

            int rank = 0;
            for (int i = 0; i < _flight.Count; i++)
            {
                if (!_flight.IsAlive(i)) continue;
                Vector3 slot = GetSlotPosition(rank++) + GetWobble(i);
                float behind = Vector3.Dot(slot - _flight.GetPosition(i), _leaderHeading);
                float speed = Data.CruiseSpeed *
                              Mathf.Clamp(1f + behind * CATCH_UP_GAIN, MIN_CATCH_UP_SPEED, MAX_CATCH_UP_SPEED);
                _flight.Steer(i, slot + _leaderHeading * SLOT_LOOKAHEAD, speed);
            }
        }

        private void TickLoiter(float deltaTime)
        {
            if (_escortAnchor != null)
            {
                _loiterCenter = _escortAnchor.position;
            }

            _orbitAngle += _orbitDirection * Data.CruiseSpeed / _loiterRadius * deltaTime;
            float trailAngle = Data.FormationSpacing * ORBIT_TRAIL_SPACINGS / _loiterRadius;
            int rank = 0;
            for (int i = 0; i < _flight.Count; i++)
            {
                if (!_flight.IsAlive(i)) continue;
                float angle = _orbitAngle + _orbitDirection * (ORBIT_LEAD_ANGLE - rank * trailAngle);
                float lane = rank == 0 ? 0f : (rank % 2 == 0 ? 0.5f : -0.5f) * Data.FormationSpacing;
                Vector3 point = _loiterCenter +
                                new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (_loiterRadius + lane);
                point.y = Data.Height;
                _flight.Steer(i, point + GetWobble(i), Data.CruiseSpeed);
                rank++;
            }
        }

        private void TickEngage(float deltaTime)
        {
            _aimRefreshTimer -= deltaTime;
            if (_aimRefreshTimer <= 0f)
            {
                _aimRefreshTimer = AIM_REFRESH_INTERVAL;
                _aimUnits = _target.HealthModel.GetShipUnits(HardPointType.Any);
            }

            int rank = 0;
            for (int i = 0; i < _flight.Count; i++)
            {
                if (!_flight.IsAlive(i)) continue;
                Vector3 aimPoint = _aimUnits.Length == 0
                    ? _target.GetFacade<IEntityTransformFacade>().Transform.position
                    : _aimUnits[rank % _aimUnits.Length].Position;
                System.Numerics.Vector3 steering = _maneuvers[i].Resolve(
                    _flight.GetPosition(i).ToNumerics(), _flight.GetForward(i).ToNumerics(),
                    aimPoint.ToNumerics(), _targetRadius, Data, out float speed);
                _flight.Steer(i, steering.ToUnity(), speed);
                rank++;
            }
        }

        private Vector3 GetSlotPosition(int rank)
        {
            System.Numerics.Vector3 offset = SquadronFormation.ToWorld(
                SquadronFormation.GetSlot(rank, Data.FormationSpacing), _leaderHeading.ToNumerics());
            return _leaderPosition + offset.ToUnity();
        }

        private float GetSlotLag()
        {
            float lag = 0f;
            int rank = 0;
            for (int i = 0; i < _flight.Count; i++)
            {
                if (!_flight.IsAlive(i)) continue;
                lag = Mathf.Max(lag, Vector3.Distance(GetSlotPosition(rank++), _flight.GetPosition(i)));
            }

            return lag;
        }

        private Vector3 GetWobble(int index)
        {
            float phase = index * 1.7f;
            float amplitude = Data.FormationSpacing * WOBBLE_SHARE;
            return new Vector3(
                Mathf.Sin(_time * 0.9f + phase) * amplitude,
                Mathf.Sin(_time * 1.3f + phase * 2f) * amplitude * 0.6f,
                Mathf.Sin(_time * 0.7f + phase * 0.5f) * amplitude * 0.5f);
        }
    }
}
