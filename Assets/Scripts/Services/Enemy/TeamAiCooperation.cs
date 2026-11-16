using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Per-team cooperation state shared by allied AI commanders: capture reservations, main-force strike
    /// strength, the shared focus enemy and station-defense requests. Human allies never publish here.
    /// </summary>
    public sealed class TeamAiCooperation
    {
        /// <summary>Published data older than this is ignored: twice the slowest decision interval (4 s) plus slack.</summary>
        public const float STALE_SECONDS = 10f;

        private readonly Dictionary<PlayerId, HashSet<string>> _reservations = new Dictionary<PlayerId, HashSet<string>>();
        private readonly Dictionary<PlayerId, Strike> _strikes = new Dictionary<PlayerId, Strike>();
        private readonly Dictionary<PlayerId, DefenseRequest> _defenseRequests = new Dictionary<PlayerId, DefenseRequest>();

        public PlayerId FocusEnemy { get; set; }

        public void PublishReservations(PlayerId player, IEnumerable<string> objectiveIds)
        {
            HashSet<string> reserved = GetReservations(player);
            reserved.Clear();
            reserved.UnionWith(objectiveIds);
        }

        public void AddReservations(PlayerId player, IEnumerable<string> objectiveIds) =>
            GetReservations(player).UnionWith(objectiveIds);

        /// <summary>
        /// True when an ally holds <paramref name="objectiveId"/>. When both hold it, the lower player index keeps it,
        /// so a simultaneous claim resolves to one owner instead of both releasing.
        /// </summary>
        public bool IsReservedByAlly(PlayerId player, string objectiveId)
        {
            bool heldBySelf = _reservations.TryGetValue(player, out HashSet<string> own) && own.Contains(objectiveId);
            foreach (KeyValuePair<PlayerId, HashSet<string>> pair in _reservations)
            {
                if (pair.Key == player || !pair.Value.Contains(objectiveId)) continue;
                if (!heldBySelf || pair.Key.Index < player.Index) return true;
            }
            return false;
        }

        public void PublishStrike(PlayerId player, ForceComposition force, PlayerId focusEnemy, Vector3 center,
            bool available, float now)
        {
            if (!_strikes.TryGetValue(player, out Strike strike))
            {
                strike = new Strike();
                _strikes.Add(player, strike);
            }
            strike.Force.CopyFrom(force);
            strike.FocusEnemy = focusEnemy;
            strike.Center = center;
            strike.Available = available;
            strike.PublishedAt = now;
        }

        /// <summary>Adds every other ally's fresh, available main force aimed at the same focus enemy.</summary>
        public void AddAlliedStrikeStrength(ForceComposition target, PlayerId player, PlayerId focusEnemy, float now)
        {
            if (focusEnemy.IsNone) return;
            foreach (KeyValuePair<PlayerId, Strike> pair in _strikes)
            {
                Strike strike = pair.Value;
                if (pair.Key == player || !strike.Available || strike.FocusEnemy != focusEnemy ||
                    now - strike.PublishedAt > STALE_SECONDS) continue;
                target.Add(strike.Force);
            }
        }

        public void RequestDefense(PlayerId owner, Vector3 station, float now) =>
            _defenseRequests[owner] = new DefenseRequest(station, now);

        public void ClearDefense(PlayerId owner) => _defenseRequests.Remove(owner);

        /// <summary>
        /// True when <paramref name="responder"/>'s main force is the nearest fresh, available allied main force
        /// to a fresh defense request from another ally. Only one ally answers each request.
        /// </summary>
        public bool TryGetDefenseAssignment(PlayerId responder, float now, out PlayerId requester, out Vector3 station)
        {
            foreach (KeyValuePair<PlayerId, DefenseRequest> request in _defenseRequests)
            {
                if (request.Key == responder || now - request.Value.RequestedAt > STALE_SECONDS) continue;
                if (FindNearestResponder(request.Key, request.Value.Station, now) != responder) continue;
                requester = request.Key;
                station = request.Value.Station;
                return true;
            }
            requester = PlayerId.None;
            station = default;
            return false;
        }

        public void RemovePlayer(PlayerId player)
        {
            _reservations.Remove(player);
            _strikes.Remove(player);
            _defenseRequests.Remove(player);
        }

        private PlayerId FindNearestResponder(PlayerId requester, Vector3 station, float now)
        {
            PlayerId nearest = PlayerId.None;
            float nearestDistance = float.MaxValue;
            foreach (KeyValuePair<PlayerId, Strike> pair in _strikes)
            {
                Strike strike = pair.Value;
                if (pair.Key == requester || !strike.Available || strike.Force.IsEmpty ||
                    now - strike.PublishedAt > STALE_SECONDS) continue;
                Vector3 offset = strike.Center - station;
                offset.y = 0f;
                float distance = offset.sqrMagnitude;
                if (distance < nearestDistance || distance == nearestDistance && !nearest.IsNone && pair.Key.Index < nearest.Index)
                {
                    nearest = pair.Key;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }

        private HashSet<string> GetReservations(PlayerId player)
        {
            if (!_reservations.TryGetValue(player, out HashSet<string> reserved))
            {
                reserved = new HashSet<string>();
                _reservations.Add(player, reserved);
            }
            return reserved;
        }

        private sealed class Strike
        {
            public readonly ForceComposition Force = new ForceComposition();
            public PlayerId FocusEnemy;
            public Vector3 Center;
            public bool Available;
            public float PublishedAt;
        }

        private readonly struct DefenseRequest
        {
            public readonly Vector3 Station;
            public readonly float RequestedAt;

            public DefenseRequest(Vector3 station, float requestedAt)
            {
                Station = station;
                RequestedAt = requestedAt;
            }
        }
    }
}
