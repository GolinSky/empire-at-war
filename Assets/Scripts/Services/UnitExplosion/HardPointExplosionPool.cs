using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Services.UnitExplosion
{
    /// <summary>
    /// One set of hardpoint destruction explosions shared by every unit, prewarmed so a destroyed hardpoint never
    /// instantiates one. A playing explosion follows its hardpoint and is hidden while the owning unit is hidden by
    /// fog; once the unit is destroyed it stops emitting (looping fire included) and finishes where the unit was last
    /// seen before returning to the pool.
    /// </summary>
    public sealed class HardPointExplosionPool : IHardPointExplosionPool, IInitializable, ITickable, ILateDisposable
    {
        private const int PREWARM_COUNT = 16;

        private readonly ExplosionVfx _prefab;
        private readonly List<ExplosionVfx> _idle = new List<ExplosionVfx>();
        private readonly List<ActiveExplosion> _active = new List<ActiveExplosion>();
        private readonly List<ExplosionVfx> _fading = new List<ExplosionVfx>();

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

        public void LateDispose()
        {
            // In play the unloading battle scene destroys the scene-root pool in no guaranteed order; only an edit-mode
            // scope, which has no scene unload, must remove it.
            if (!Application.isPlaying) Object.DestroyImmediate(_root.gameObject);
        }

        public void Play(Transform anchor, IFogVisibilityFacade fogVisibility, float scale)
        {
            ExplosionVfx explosion = TakeIdle();
            explosion.transform.localScale = Vector3.one * scale;
            ActiveExplosion active = new ActiveExplosion(explosion, anchor, fogVisibility);
            active.Follow();
            explosion.Play();
            _active.Add(active);
        }

        public void Stop(Transform anchor)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i].Anchor == anchor) FadeAt(i);
            }
        }

        private void FadeAt(int index)
        {
            ExplosionVfx explosion = _active[index].Explosion;
            _active.RemoveAt(index);
            // Scene unload may destroy the pool's particle objects before the hardpoint disables.
            if (explosion == null || explosion.HasExpired) return;
            explosion.StopEmitting();
            _fading.Add(explosion);
        }

        public void Tick()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ActiveExplosion active = _active[i];
                if (active.Explosion == null || active.Explosion.HasExpired)
                {
                    _active.RemoveAt(i);
                    continue;
                }
                if (active.Anchor == null || !active.Anchor.gameObject.activeInHierarchy)
                {
                    FadeAt(i);
                    continue;
                }

                if (!active.Explosion.IsPlaying)
                {
                    _idle.Add(active.Explosion);
                    _active.RemoveAt(i);
                    continue;
                }

                active.Follow();
            }

            for (int i = _fading.Count - 1; i >= 0; i--)
            {
                if (_fading[i] == null || _fading[i].HasExpired)
                {
                    _fading.RemoveAt(i);
                    continue;
                }
                if (!_fading[i].IsPlaying)
                {
                    _idle.Add(_fading[i]);
                    _fading.RemoveAt(i);
                }
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

        public void Clear()
        {
            foreach (ActiveExplosion active in _active) _idle.Add(active.Explosion);
            _idle.AddRange(_fading);
            _active.Clear();
            _fading.Clear();
            foreach (ExplosionVfx explosion in _idle) explosion.Clear();
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

            public Transform Anchor => _anchor;

            public void Follow()
            {
                Explosion.transform.position = _anchor.position;
                bool isHidden = _fogVisibility != null && _fogVisibility.IsHiddenByFog;
                IReadOnlyList<Renderer> renderers = Explosion.Renderers;
                for (int i = 0; i < renderers.Count; i++)
                {
                    renderers[i].forceRenderingOff = isHidden;
                }
            }
        }
    }
}
