using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Pooling;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class GravityWellEffects : ILateDisposable
    {
        private const int MAX_IDLE_VIEWS = 4;

        private readonly IPlayerRelations _relations;
        private readonly IMatchSession _session;
        private ViewPool<GravityWellView> _pool;

        public GravityWellEffects(IPlayerRelations relations, IMatchSession session)
        {
            _relations = relations;
            _session = session;
        }

        public GravityWellView Show(GravityWellView prefab, PlayerId owner, Transform caster, float radius,
            ISelectionModelObserver selection)
        {
            _pool ??= new ViewPool<GravityWellView>(prefab, view => view.Hide(), "Gravity Wells", MAX_IDLE_VIEWS);
            GravityWellView view = _pool.Get();
            bool hostile = !_session.LocalPlayerId.IsNone && _relations.IsHostile(_session.LocalPlayerId, owner);
            view.Show(caster, radius, hostile, selection);
            return view;
        }

        public void Hide(GravityWellView view)
        {
            _pool.Release(view);
        }

        public void LateDispose()
        {
            if (_pool == null) return;
            _pool.Dispose();
            _pool = null;
        }
    }
}
