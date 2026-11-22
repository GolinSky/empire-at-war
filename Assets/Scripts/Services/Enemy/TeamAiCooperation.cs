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
        private readonly ForceComposition _combined = new ForceComposition();

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

        /// <summary>Adds every other ally's fresh, available main force whatever its focus: they meet an armada together.</summary>
        public void AddAvailableAlliedStrength(ForceComposition target, PlayerId player, float now)
        {
            foreach (KeyValuePair<PlayerId, Strike> pair in _strikes)
                if (pair.Key != player && IsAvailable(pair.Value, now)) target.Add(pair.Value.Force);
        }

        /// <summary>
        /// Asks the team for help at <paramref name="station"/>: sent from the first station pressure on.
        /// <paramref name="attackers"/> are the known hostiles at the station, <paramref name="defenders"/> the team's
        /// units and armed structures already there; null counts as none.
        /// </summary>
        public void RequestDefense(PlayerId owner, Vector3 station, float now, ForceComposition attackers = null,
            ForceComposition defenders = null)
        {
            if (!_defenseRequests.TryGetValue(owner, out DefenseRequest request))
            {
                request = new DefenseRequest();
                _defenseRequests.Add(owner, request);
            }
            request.Station = station;
            request.RequestedAt = now;
            request.Attackers.Clear();
            request.Defenders.Clear();
            if (attackers != null) request.Attackers.CopyFrom(attackers);
            if (defenders != null) request.Defenders.CopyFrom(defenders);
        }

        public void ClearDefense(PlayerId owner) => _defenseRequests.Remove(owner);

        /// <summary>True while any ally has a fresh defense request: a team station is under pressure.</summary>
        public bool HasDefenseRequest(float now)
        {
            foreach (KeyValuePair<PlayerId, DefenseRequest> request in _defenseRequests)
                if (now - request.Value.RequestedAt <= STALE_SECONDS) return true;
            return false;
        }

        /// <summary>
        /// True when <paramref name="responder"/> has a fresh, available main force and a fresh request from another
        /// ally can be won by everyone who answers: the defenders at the station plus every available allied main
        /// force reach <paramref name="minimumAdvantage"/> over the attackers. Every such main force answers, so two
        /// weak fleets go together instead of one staying away.
        /// </summary>
        public bool TryGetDefenseAssignment(PlayerId responder, float now, float minimumAdvantage,
            out PlayerId requester, out Vector3 station)
        {
            if (_strikes.TryGetValue(responder, out Strike own) && IsAvailable(own, now))
                foreach (KeyValuePair<PlayerId, DefenseRequest> request in _defenseRequests)
                {
                    if (request.Key == responder || now - request.Value.RequestedAt > STALE_SECONDS ||
                        !CanWin(request.Key, request.Value, now, minimumAdvantage)) continue;
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

        private bool CanWin(PlayerId requester, DefenseRequest request, float now, float minimumAdvantage)
        {
            if (request.Attackers.IsEmpty) return true;
            _combined.CopyFrom(request.Defenders);
            foreach (KeyValuePair<PlayerId, Strike> pair in _strikes)
                if (pair.Key != requester && IsAvailable(pair.Value, now)) _combined.Add(pair.Value.Force);
            return CombatMatchup.Advantage(_combined, request.Attackers) >= minimumAdvantage;
        }

        private static bool IsAvailable(Strike strike, float now) =>
            strike.Available && !strike.Force.IsEmpty && now - strike.PublishedAt <= STALE_SECONDS;

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

        private sealed class DefenseRequest
        {
            public readonly ForceComposition Attackers = new ForceComposition();
            public readonly ForceComposition Defenders = new ForceComposition();
            public Vector3 Station;
            public float RequestedAt;
        }
    }
}
