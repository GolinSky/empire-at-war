using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.OwnedAreas;
using UnityEngine;

namespace EmpireAtWar.Services.Vision
{
    public sealed class VisionService : IVisionService
    {
        private readonly IPlayerRelations _relations;

        private readonly OwnedCircleSet _sources = new OwnedCircleSet();

        public IReadOnlyList<OwnedCircle> Sources => _sources.Circles;

        public VisionService(IPlayerRelations relations)
        {
            _relations = relations;
        }

        public void Register(PlayerId owner, Transform source, float radius) =>
            _sources.Register(owner, source, radius);

        public void Unregister(Transform source) => _sources.Unregister(source);

        public bool IsVisible(PlayerId viewer, Vector3 position) =>
            _sources.AnyContains(position, 0f, _relations, viewer, true);

        public bool IsAreaVisible(PlayerId viewer, Vector3 position, float radius) =>
            _sources.AnyContains(position, radius, _relations, viewer, true);
    }
}
