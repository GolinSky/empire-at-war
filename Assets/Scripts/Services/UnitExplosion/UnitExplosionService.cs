using System;
using System.Collections.Generic;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Pooling;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.UnitExplosion
{
    public sealed class UnitExplosionService : Service, IUnitExplosionService, ITickable, IDisposable
    {
        private const float COVERAGE_PADDING = 1.2f;

        private const int MAX_IDLE_EXPLOSIONS = 32;

        private readonly UnitExplosionView _prefab;
        private readonly ViewPool<UnitExplosionView> _pool;
        private readonly List<UnitExplosionView> _active = new List<UnitExplosionView>();

        public UnitExplosionService(UnitExplosionView prefab)
        {
            _prefab = prefab;
            _pool = new ViewPool<UnitExplosionView>(prefab: prefab, rootName: "UnitExplosions", hide: view => view.Hide(),
                maxSize: MAX_IDLE_EXPLOSIONS);
        }

        public void Dispose()
        {
            foreach (UnitExplosionView view in _active)
            {
                _pool.Release(view);
            }

            _active.Clear();
            _pool.Dispose();
        }

        public void Spawn(IReadOnlyList<Renderer> hullRenderers)
        {
            Bounds bounds = hullRenderers[0].bounds;
            for (int i = 1; i < hullRenderers.Count; i++)
            {
                bounds.Encapsulate(hullRenderers[i].bounds);
            }

            // The enclosing sphere covers every corner, even for a long or rotated hull.
            float diameter = bounds.extents.magnitude * 2f * COVERAGE_PADDING;
            UnitExplosionView view = _pool.Get();
            view.Play(bounds.center, diameter / _prefab.Diameter);
            _active.Add(view);
        }

        public void Tick()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (!_active[i].IsAlive)
                {
                    _pool.Release(_active[i]);
                    _active.RemoveAt(i);
                }
            }
        }
    }
}
