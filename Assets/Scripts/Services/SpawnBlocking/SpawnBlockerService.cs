using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.OwnedAreas;
using UnityEngine;

namespace EmpireAtWar.Services.SpawnBlocking
{
    public sealed class SpawnBlockerService : ISpawnBlockerService
    {
        private readonly IPlayerRelations _relations;

        private readonly OwnedCircleSet _blockers = new OwnedCircleSet();

        public IReadOnlyList<OwnedCircle> Blockers => _blockers.Circles;

        public SpawnBlockerService(IPlayerRelations relations)
        {
            _relations = relations;
        }

        public void Register(PlayerId owner, Transform source, float radius) =>
            _blockers.Register(owner, source, radius);

        public void Unregister(Transform source) => _blockers.Unregister(source);

        public bool IsBlocked(PlayerId team, Vector3 position) =>
            _blockers.AnyContains(position, 0f, _relations, team, false);
    }
}
