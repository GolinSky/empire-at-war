using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Pooling;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    /// <summary>
    /// Pooled range rings and the active hull emitters of running gravity wells. Emitters off the battle camera stop
    /// spawning particles until they return to view.
    /// </summary>
    public sealed class GravityWellEffects : ITickable, ILateDisposable
    {
        private const int MAX_IDLE_VIEWS = 4;

        private readonly IPlayerRelations _relations;
        private readonly IMatchSession _session;
        private readonly UnityEngine.Camera _camera;
        private readonly Plane[] _planes = new Plane[6];
        private readonly List<IGravityWellEmitterFacade> _emitters = new List<IGravityWellEmitterFacade>();
        private ViewPool<GravityWellView> _pool;

        public GravityWellEffects(IPlayerRelations relations, IMatchSession session, UnityEngine.Camera camera)
        {
            _relations = relations;
            _session = session;
            _camera = camera;
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

        public void StartEmitter(IGravityWellEmitterFacade emitter)
        {
            _emitters.Add(emitter);
            emitter.SetEmitting(true);
        }

        public void StopEmitter(IGravityWellEmitterFacade emitter)
        {
            _emitters.Remove(emitter);
            emitter.SetEmitting(false);
        }

        public void Tick()
        {
            if (_emitters.Count == 0) return;
            GeometryUtility.CalculateFrustumPlanes(_camera, _planes);
            for (int i = 0; i < _emitters.Count; i++)
                _emitters[i].SetOnScreen(GeometryUtility.TestPlanesAABB(_planes, _emitters[i].CullBounds));
        }

        public void LateDispose()
        {
            _emitters.Clear();
            if (_pool == null) return;
            _pool.Dispose();
            _pool = null;
        }
    }
}
