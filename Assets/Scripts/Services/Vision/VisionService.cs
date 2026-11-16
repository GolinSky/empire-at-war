using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using System;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.OwnedAreas;
using UnityEngine;

namespace EmpireAtWar.Services.Vision
{
    public sealed class VisionService : IVisionService, ITickable
    {
        public const float FIRING_REVEAL_SECONDS = 5f;

        private readonly IPlayerRelations _relations;
        private readonly Func<float> _time;
        private readonly OwnedCircleSet _sources = new OwnedCircleSet();
        private readonly Dictionary<long, Dictionary<PlayerId, float>> _reveals =
            new Dictionary<long, Dictionary<PlayerId, float>>();
        private readonly List<VisionReveal> _expired = new List<VisionReveal>();

        public event Action<PlayerId, long, bool> RevealChanged;
        public bool FogEnabled { get; }
        public IReadOnlyList<OwnedCircle> Sources => _sources.Circles;

        public VisionService(IPlayerRelations relations, bool fogEnabled = true)
            : this(relations, fogEnabled, () => Time.time) { }

        public VisionService(IPlayerRelations relations, bool fogEnabled, Func<float> time)
        {
            _relations = relations;
            FogEnabled = fogEnabled;
            _time = time;
        }

        public void Register(PlayerId owner, Transform source, float radius) =>
            _sources.Register(owner, source, radius);

        public void Unregister(Transform source) => _sources.Unregister(source);

        public bool IsVisible(PlayerId viewer, Vector3 position) =>
            !FogEnabled || _sources.AnyContains(position, 0f, _relations, viewer, true);

        public bool IsVisible(PlayerId viewer, IVisionFootprintFacade footprint)
        {
            if (!FogEnabled || IsRevealed(viewer, footprint.EntityId)) return true;
            return _sources.AnyIntersects(footprint.WorldHull, footprint.WorldBounds, _relations, viewer);
        }

        public bool IsAreaVisible(PlayerId viewer, Vector3 position, float radius) =>
            !FogEnabled || _sources.AnyContains(position, radius, _relations, viewer, true);

        public void Reveal(PlayerId viewer, long entityId, float duration)
        {
            if (FogEnabled) SetReveal(viewer, entityId, _time() + duration);
        }

        public void SetReplicaReveal(PlayerId viewer, long entityId, bool active)
        {
            if (active) SetReveal(viewer, entityId, float.PositiveInfinity);
            else RemoveReveal(viewer, entityId);
        }

        public void CopyActiveReveals(List<VisionReveal> output)
        {
            Tick();
            output.Clear();
            foreach (var entity in _reveals)
                foreach (var viewer in entity.Value)
                    output.Add(new VisionReveal(viewer.Key, entity.Key));
        }

        public void ForgetReveals(long entityId)
        {
            if (!_reveals.TryGetValue(entityId, out Dictionary<PlayerId, float> viewers)) return;
            _reveals.Remove(entityId);
            foreach (PlayerId viewer in viewers.Keys) RevealChanged?.Invoke(viewer, entityId, false);
        }

        public void Tick()
        {
            float now = _time();
            _expired.Clear();
            foreach (var entity in _reveals)
                foreach (var viewer in entity.Value)
                    if (viewer.Value <= now) _expired.Add(new VisionReveal(viewer.Key, entity.Key));
            for (int i = 0; i < _expired.Count; i++)
                RemoveReveal(_expired[i].Viewer, _expired[i].EntityId);
        }

        private bool IsRevealed(PlayerId viewer, long entityId)
        {
            if (!_reveals.TryGetValue(entityId, out Dictionary<PlayerId, float> viewers)) return false;
            foreach (PlayerId revealedTo in viewers.Keys)
                if (_relations.IsAllied(viewer, revealedTo)) return true;
            return false;
        }

        private void SetReveal(PlayerId viewer, long entityId, float expiresAt)
        {
            if (!_reveals.TryGetValue(entityId, out Dictionary<PlayerId, float> viewers))
            {
                viewers = new Dictionary<PlayerId, float>();
                _reveals.Add(entityId, viewers);
            }
            bool wasRevealed = viewers.ContainsKey(viewer);
            viewers[viewer] = expiresAt;
            if (!wasRevealed) RevealChanged?.Invoke(viewer, entityId, true);
        }

        private void RemoveReveal(PlayerId viewer, long entityId)
        {
            if (!_reveals.TryGetValue(entityId, out Dictionary<PlayerId, float> viewers) ||
                !viewers.Remove(viewer)) return;
            if (viewers.Count == 0) _reveals.Remove(entityId);
            RevealChanged?.Invoke(viewer, entityId, false);
        }
    }
}
