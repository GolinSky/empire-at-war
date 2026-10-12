using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.UnitExplosion
{
    /// <summary>
    /// One set of hardpoint destruction explosions shared by every unit, prewarmed so a destroyed hardpoint never
    /// instantiates one. A playing explosion follows its hardpoint and is hidden while the owning unit is hidden by
    /// fog; once the unit is destroyed it finishes where the unit was last seen.
    /// </summary>
    public sealed class HardPointExplosionPool : IHardPointExplosionPool, IInitializable, ITickable
    {
        private const int PREWARM_COUNT = 16;

        private readonly ExplosionVfx _prefab;
        private readonly List<ExplosionVfx> _idle = new List<ExplosionVfx>();
        private readonly List<ActiveExplosion> _active = new List<ActiveExplosion>();

        private Transform _root;

        public HardPointExplosionPool(ExplosionVfx prefab)
        {
            _prefab = prefab;
        }

        public void Initialize()
        {
            _root = new GameObject(nameof(HardPointExplosionPool)).transform;
            for (int i = 0; i < PREWARM_COUNT; i++)
            {
                _idle.Add(Object.Instantiate(_prefab, _root));
            }
        }

        public void Play(Transform anchor, IFogVisibilityFacade fogVisibility)
        {
            ExplosionVfx explosion = TakeIdle();
            ActiveExplosion active = new ActiveExplosion(explosion, anchor, fogVisibility);
            active.Follow();
            explosion.Play();
            _active.Add(active);
        }

        public void Tick()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ActiveExplosion active = _active[i];
                if (!active.Explosion.IsPlaying)
                {
                    _idle.Add(active.Explosion);
                    _active.RemoveAt(i);
                    continue;
                }

                active.Follow();
            }
        }

        private ExplosionVfx TakeIdle()
        {
            int last = _idle.Count - 1;
            if (last < 0)
            {
                return Object.Instantiate(_prefab, _root);
            }

            ExplosionVfx explosion = _idle[last];
            _idle.RemoveAt(last);
            return explosion;
        }

        private readonly struct ActiveExplosion
        {
            private readonly Transform _anchor;
            private readonly IFogVisibilityFacade _fogVisibility;

            public ExplosionVfx Explosion { get; }

            public ActiveExplosion(ExplosionVfx explosion, Transform anchor, IFogVisibilityFacade fogVisibility)
            {
                Explosion = explosion;
                _anchor = anchor;
                _fogVisibility = fogVisibility;
            }

            public void Follow()
            {
                // The unit is destroyed shortly after its death; the explosion stays where it last was.
                if (_anchor == null)
                {
                    return;
                }

                Explosion.transform.position = _anchor.position;
                bool isHidden = _fogVisibility != null && _fogVisibility.IsHiddenByFog;
                foreach (Renderer explosionRenderer in Explosion.Renderers)
                {
                    explosionRenderer.forceRenderingOff = isHidden;
                }
            }
        }
    }
}
