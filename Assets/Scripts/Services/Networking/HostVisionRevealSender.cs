using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.Vision;
using Mirror;

namespace EmpireAtWar.Services.Networking
{
    public sealed class HostVisionRevealSender : IInitializable, ILateDisposable
    {
        private readonly VisionService _vision;
        private readonly IEntityLocator _entities;
        private readonly OnlineMatchService _online;
        private readonly NetworkTickService _ticks;
        private readonly NetworkEventSequence _events = new NetworkEventSequence();
        private readonly Dictionary<(PlayerId Viewer, long EntityId), (long Sequence, VisionRevealMessage Message)>
            _pending = new Dictionary<(PlayerId, long), (long, VisionRevealMessage)>();
        private readonly List<VisionReveal> _baseline = new List<VisionReveal>();

        public HostVisionRevealSender(VisionService vision, IEntityLocator entities, OnlineMatchService online,
            NetworkTickService ticks)
        {
            _vision = vision;
            _entities = entities;
            _online = online;
            _ticks = ticks;
        }

        public void Initialize()
        {
            _vision.RevealChanged += OnRevealChanged;
            _entities.EntityRemoved += OnEntityRemoved;
            _ticks.SpawnsSent += Flush;
        }

        public void LateDispose()
        {
            _vision.RevealChanged -= OnRevealChanged;
            _entities.EntityRemoved -= OnEntityRemoved;
            _ticks.SpawnsSent -= Flush;
            _pending.Clear();
        }

        public void SendBaseline(NetworkConnectionToClient connection)
        {
            _baseline.Clear();
            _vision.CopyActiveReveals(_baseline);
            _events.MarkBaseline(connection.connectionId);
            foreach (VisionReveal reveal in _baseline)
                connection.Send(new VisionRevealMessage
                    { Viewer = reveal.Viewer, EntityId = reveal.EntityId, Active = true });
        }

        private void OnEntityRemoved(IEntity entity) => _vision.ForgetReveals(entity.Id);

        private void OnRevealChanged(PlayerId viewer, long entityId, bool active)
        {
            _pending[(viewer, entityId)] = (_events.Next(), new VisionRevealMessage
                { Viewer = viewer, EntityId = entityId, Active = active });
        }

        private void Flush()
        {
            if (!_online.HasBegun) return;
            foreach (var pending in _pending.Values)
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
                if (_online.IsBattleLoaded(connection) &&
                    _events.IsAfterBaseline(connection.connectionId, pending.Sequence))
                    connection.Send(pending.Message);
            _pending.Clear();
        }
    }
}
