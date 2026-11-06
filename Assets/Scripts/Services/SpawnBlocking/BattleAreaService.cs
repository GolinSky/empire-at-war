using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.OwnedAreas;
using UnityEngine;

namespace EmpireAtWar.Services.SpawnBlocking
{
    public sealed class BattleAreaService : IBattleAreaService, IBattleAreaWriter
    {
        private readonly IPlayerRelations _relations;
        private readonly OwnedCircleSet[] _layers = { new OwnedCircleSet(), new OwnedCircleSet(), new OwnedCircleSet() };

        public BattleAreaService(IPlayerRelations relations)
        {
            _relations = relations;
        }

        public void Register(BattleAreaLayer layer, PlayerId owner, Transform source, float radius) =>
            _layers[(int)layer].Register(owner, source, radius);

        public void Unregister(BattleAreaLayer layer, Transform source) =>
            _layers[(int)layer].Unregister(source);

        public IReadOnlyList<OwnedCircle> Get(BattleAreaLayer layer) => _layers[(int)layer].Circles;

        public bool IsBlocked(PlayerId team, Vector3 position) =>
            _layers[(int)BattleAreaLayer.SpawnBlock].AnyContains(position, 0f, _relations, team, false);

        public bool IsStructureExcluded(Vector3 position, float margin) =>
            TryFind(BattleAreaLayer.StructureExclusion, position, margin, out _);

        public bool TryGetOrderSnap(Vector3 position, out Vector3 center) =>
            TryFind(BattleAreaLayer.OrderSnap, position, 0f, out center);

        private bool TryFind(BattleAreaLayer layer, Vector3 position, float margin, out Vector3 center)
        {
            IReadOnlyList<OwnedCircle> circles = Get(layer);
            for (int i = 0; i < circles.Count; i++)
            {
                OwnedCircle circle = circles[i];
                if (!circle.Contains(position, margin)) continue;
                center = circle.Transform.position;
                return true;
            }

            center = default;
            return false;
        }
    }
}
