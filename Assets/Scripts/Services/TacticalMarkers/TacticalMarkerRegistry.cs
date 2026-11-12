using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.PlayerCommands;
using EmpireAtWar.Services.Saves;
using UnityEngine;

namespace EmpireAtWar.Services.TacticalMarkers
{
    public sealed class TacticalMarkerRegistry : ITacticalMarkerObserver, ITacticalMarkerCommandHandler,
        IInitializable, ITickable, IDisposable, IObserver<BattleState>
    {
        private const float TRACK_INTERVAL = 0.2f;
        private readonly TacticalMarkerSettings _settings;
        private readonly IPlayerRoster _players;
        private readonly TacticalMarkerTargets _targets;
        private readonly IMapModelObserver _map;
        private readonly IMatchSession _session;
        private readonly BattleClock _clock;
        private readonly PlayerCommandHandlers _commands;
        private readonly INotifier<BattleState> _battleState;
        private readonly List<TacticalMarker> _markers = new List<TacticalMarker>();
        private readonly Dictionary<PlayerId, Queue<float>> _placements = new Dictionary<PlayerId, Queue<float>>();
        private long _nextId;
        private float _nextTrack;
        private bool _ended;
        public IReadOnlyList<TacticalMarker> Markers => _markers;
        public float Time => _clock.Elapsed;
        public event Action<TacticalMarker> Added;
        public event Action<TacticalMarker> Updated;
        public event Action<TacticalMarker> Removed;

        public TacticalMarkerRegistry(TacticalMarkerSettings settings, IPlayerRoster players,
            TacticalMarkerTargets targets, IMapModelObserver map, IMatchSession session, BattleClock clock,
            PlayerCommandHandlers commands, INotifier<BattleState> battleState)
        {
            _settings = settings;
            _players = players;
            _targets = targets;
            _map = map;
            _session = session;
            _clock = clock;
            _commands = commands;
            _battleState = battleState;
            _clock.Restored += Clear;
        }

        public void Initialize()
        {
            if (_session.IsAuthority) _commands.RegisterTacticalMarkers(this);
            _battleState.AddObserver(this);
        }

        public void UpdateState(BattleState state)
        {
            _ended = state == BattleState.Ended;
            if (_ended || state == BattleState.Loading) Clear();
        }

        public bool TryPlace(PlaceMarkerCommand command)
        {
            if (_ended || !_session.IsAuthority || command.Type > TacticalMarkerType.Retreat || !IsFinite(command.Point)) return false;
            PlayerSlot sender = null;
            foreach (PlayerSlot player in _players.Players)
                if (player.Id == command.Issuer) { sender = player; break; }
            if (sender == null || !AllowPlacement(sender.Id)) return false;
            Vector3 point = Clamp(command.Point);
            long targetId = command.TargetId;
            if (_targets.TryGetVisiblePoint(sender.Id, targetId, out Vector3 targetPoint, out _)) point = Clamp(targetPoint);
            else targetId = TacticalMarkerTarget.NO_TARGET;
            TacticalMarkerType type = command.Type;
            ICaptureSite site = _targets.FindSite(targetId);
            if (type == TacticalMarkerType.Capture && site != null && _players.IsAllied(sender.Id, site.Owner))
                type = TacticalMarkerType.Defend;
            TacticalMarker oldest = null;
            int count = 0;
            foreach (TacticalMarker marker in _markers)
            {
                if (marker.Sender != sender.Id) continue;
                count++;
                if (oldest == null || marker.CreatedAt < oldest.CreatedAt) oldest = marker;
                if (marker.Type != type || Time - marker.CreatedAt >= _settings.RateWindow ||
                    (marker.Point - point).sqrMagnitude >= _settings.RepeatRadius * _settings.RepeatRadius) continue;
                marker.Point = point;
                marker.TargetId = targetId;
                marker.CreatedAt = Time;
                marker.ExpiresAt = Time + _settings.GetLifetime(type);
                Updated?.Invoke(marker);
                return true;
            }
            if (count >= _settings.MaxPerPlayer) Remove(oldest, TacticalMarkerState.Replaced);
            TacticalMarker added = new TacticalMarker(++_nextId, type, sender.Id, sender.Team, point, targetId,
                Time, Time + _settings.GetLifetime(type));
            _markers.Add(added);
            Added?.Invoke(added);
            return true;
        }

        private bool AllowPlacement(PlayerId sender)
        {
            if (!_placements.TryGetValue(sender, out Queue<float> recent))
                _placements.Add(sender, recent = new Queue<float>());
            while (recent.Count > 0 && Time - recent.Peek() >= _settings.RateWindow) recent.Dequeue();
            if (recent.Count >= _settings.RateLimit) return false;
            recent.Enqueue(Time);
            return true;
        }

        public void Tick()
        {
            bool track = _session.IsAuthority && Time >= _nextTrack;
            if (track) _nextTrack = Time + TRACK_INTERVAL;
            for (int i = _markers.Count - 1; i >= 0; i--)
            {
                TacticalMarker marker = _markers[i];
                if (Time >= marker.ExpiresAt) { Remove(marker, TacticalMarkerState.Expired); continue; }
                if (!track || marker.TargetId == TacticalMarkerTarget.NO_TARGET) continue;
                if (_targets.TryGetVisiblePoint(marker.Sender, marker.TargetId, out Vector3 point, out bool exists))
                {
                    point = Clamp(point);
                    if ((point - marker.Point).sqrMagnitude < 0.01f) continue;
                    marker.Point = point;
                }
                else if (exists) continue;
                else marker.TargetId = TacticalMarkerTarget.NO_TARGET;
                Updated?.Invoke(marker);
            }
        }

        public void Resolve(long id) => RemoveMirror(id, TacticalMarkerState.Resolved);

        public void ApplyMirror(long id, TacticalMarkerType type, PlayerId sender, Vector3 point, long targetId,
            float age, float remaining)
        {
            if (_ended || _session.IsAuthority || !_players.IsAllied(_session.LocalPlayerId, sender)) return;
            foreach (TacticalMarker marker in _markers)
            {
                if (marker.Id != id) continue;
                marker.Point = point;
                marker.TargetId = targetId;
                marker.CreatedAt = Time - age;
                marker.ExpiresAt = Time + remaining;
                Updated?.Invoke(marker);
                return;
            }
            TacticalMarker added = new TacticalMarker(id, type, sender, _players.Get(sender).Team, point,
                targetId, Time - age, Time + remaining);
            _markers.Add(added);
            Added?.Invoke(added);
        }

        public void RemoveMirror(long id, TacticalMarkerState state)
        {
            for (int i = _markers.Count - 1; i >= 0; i--)
                if (_markers[i].Id == id) { Remove(_markers[i], state); return; }
        }

        private void Remove(TacticalMarker marker, TacticalMarkerState state)
        {
            marker.State = state;
            _markers.Remove(marker);
            Removed?.Invoke(marker);
        }

        public void Dispose()
        {
            if (_session.IsAuthority) _commands.UnregisterTacticalMarkers();
            _battleState.RemoveObserver(this);
            _clock.Restored -= Clear;
            Clear();
        }

        public void Clear()
        {
            while (_markers.Count > 0) Remove(_markers[_markers.Count - 1], TacticalMarkerState.Expired);
            _placements.Clear();
            _nextTrack = 0;
        }

        private Vector3 Clamp(Vector3 point)
        {
            Vector2 clamped = _map.SizeRange.Clamp(new Vector2(point.x, point.z));
            return new Vector3(clamped.x, point.y, clamped.y);
        }

        private static bool IsFinite(Vector3 point) =>
            !float.IsNaN(point.x) && !float.IsInfinity(point.x) &&
            !float.IsNaN(point.y) && !float.IsInfinity(point.y) &&
            !float.IsNaN(point.z) && !float.IsInfinity(point.z);
    }
}
