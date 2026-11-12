using System.Collections.Generic;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.BattleNotifications;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Services.TacticalMarkers;
using UnityEngine;

namespace EmpireAtWar.Entities.TacticalMarkers
{
    /// <summary>Shows allied markers in the world: pools <see cref="WorldMarkerView"/>s, plays cues and posts cards.</summary>
    public sealed class WorldMarkerController : ILateTickable
    {
        private readonly ITacticalMarkerObserver _markers;
        private readonly TacticalMarkerSettings _settings;
        private readonly ILocalPlayer _local;
        private readonly IPlayerColors _colors;
        private readonly ICameraService _camera;
        private readonly IBattleNotifications _notifications;
        private readonly IAudioService _audio;
        private readonly TacticalMarkerReplies _replies;
        private readonly EmpireAtWar.Services.Networking.Online.OnlineMatchService _online;
        private readonly IPlayerRoster _roster;
        // Markers older than this on arrival came from a late-join snapshot and are shown silently.
        private const float FRESH_MARKER_SECONDS = 0.5f;
        private readonly Dictionary<long, WorldMarkerView> _active = new Dictionary<long, WorldMarkerView>();
        private readonly Stack<WorldMarkerView> _pool = new Stack<WorldMarkerView>();
        private WorldMarkerView _prefab;
        private AudioSource _source;
        private bool _initialized;

        public WorldMarkerController(ITacticalMarkerObserver markers, TacticalMarkerSettings settings,
            ILocalPlayer local, IPlayerColors colors, ICameraService camera, IBattleNotifications notifications,
            IAudioService audio, TacticalMarkerReplies replies,
            EmpireAtWar.Services.Networking.Online.OnlineMatchService online, IPlayerRoster roster)
        {
            _markers = markers; _settings = settings; _local = local; _colors = colors;
            _camera = camera; _notifications = notifications; _audio = audio; _replies = replies;
            _online = online; _roster = roster;
        }

        public void Initialize(WorldMarkerView prefab, AudioSource source)
        {
            _prefab = prefab; _source = source; _initialized = true;
            _markers.Added += Added; _markers.Removed += Removed; _replies.Replied += Replied;
            foreach (TacticalMarker marker in _markers.Markers) Added(marker);
        }

        private void Added(TacticalMarker marker)
        {
            if (!_local.IsFriendly(marker.Sender)) return;
            WorldMarkerView view = _pool.Count > 0 ? _pool.Pop() : Object.Instantiate(_prefab);
            _active.Add(marker.Id, view);
            view.Show(_settings.GetIcon(marker.Type), _settings.OctagonFrame, _settings.GetColor(marker.Type),
                Name(marker.Sender), _colors.GetColor(marker.Sender));
            if (_markers.Time - marker.CreatedAt > FRESH_MARKER_SECONDS) return;
            _audio.PlayOneShot(_source, _settings.GetSound(marker.Type), 0.65f);
            if (_local.IsLocal(marker.Sender)) return;
            bool urgent = marker.Type is TacticalMarkerType.Attack or TacticalMarkerType.Defend or TacticalMarkerType.Retreat;
            _notifications.Post(urgent ? BattleNotificationPriority.Urgent : BattleNotificationPriority.Progress,
                $"{Name(marker.Sender)}: {marker.Type} here", iconKey: "Marker" + marker.Type,
                onActivate: () => _camera.MoveTo(marker.Point));
        }

        private void Replied(TacticalMarkerReply reply)
        {
            if (!_local.IsFriendly(reply.Sender)) return;
            string text = reply.Status switch
            {
                TacticalMarkerReplyStatus.Accepted => "On my way",
                TacticalMarkerReplyStatus.Delayed => "Busy now — will come when free",
                TacticalMarkerReplyStatus.Declined => "Too strong — need support",
                _ => "Capture complete"
            };
            _notifications.Post(BattleNotificationPriority.Progress, $"{Name(reply.Responder)}: {text}",
                iconKey: "Marker" + reply.Type, onActivate: reply.Status == TacticalMarkerReplyStatus.Captured
                    ? null : () => _camera.MoveTo(reply.Point));
        }

        private string Name(PlayerId player)
        {
            foreach (var member in _online.Members)
                if (member.SlotIndex == player.Index) return member.Name;
            PlayerSlot slot = _roster.Get(player);
            return slot.IsAi ? $"{slot.Faction} AI" : $"Player {player.Index + 1}";
        }
        private void Removed(TacticalMarker marker)
        {
            if (!_active.Remove(marker.Id, out WorldMarkerView view)) return;
            view.Release(); _pool.Push(view);
        }

        public void LateTick()
        {
            if (!_initialized) return;
            foreach (TacticalMarker marker in _markers.Markers)
                if (_active.TryGetValue(marker.Id, out WorldMarkerView view))
                    view.Render(marker.Point, _camera.SceneCamera, _markers.Time - marker.CreatedAt, marker.ExpiresAt - _markers.Time);
        }

        public void LateDispose()
        {
            if (!_initialized) return;
            _initialized = false;
            _markers.Added -= Added; _markers.Removed -= Removed; _replies.Replied -= Replied;
            foreach (WorldMarkerView view in _active.Values) if (view != null) view.Destroy();
            foreach (WorldMarkerView view in _pool) if (view != null) view.Destroy();
            _active.Clear(); _pool.Clear();
        }
    }
}
