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
        private readonly ExplosionVfx _prefab;
        private readonly int _prewarmCount;
        private readonly UnityEngine.Camera _camera;
        private readonly Plane[] _planes = new Plane[6];
        private readonly List<ExplosionVfx> _idle = new List<ExplosionVfx>();
        private readonly List<ActiveExplosion> _active = new List<ActiveExplosion>();
        private readonly List<ExplosionVfx> _fading = new List<ExplosionVfx>();

        private Transform _root;
        private int _peakInUse;
        private int _grownCount;

        public HardPointExplosionPool(ExplosionVfx prefab, UnityEngine.Camera camera, int prewarmCount)
        {
            _prefab = prefab;
            _camera = camera;
            _prewarmCount = prewarmCount;
        }

        public void Initialize()
        {
            _root = new GameObject(_prefab.name + "Pool").transform;
            for (int i = 0; i < _prewarmCount; i++)
            {
                _idle.Add(Object.Instantiate(_prefab, _root));
            }
        }

        public void LateDispose()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Data for sizing the prewarm count: peak effects in use at once, and instances created mid-battle.
            if (Application.isPlaying)
                Debug.Log($"[VfxPools] {_prefab.name} prewarm={_prewarmCount} peak={_peakInUse} grown={_grownCount}");
#endif
            // In play the unloading battle scene destroys the scene-root pool in no guaranteed order; only an edit-mode
            // scope, which has no scene unload, must remove it.
            if (!Application.isPlaying && _root != null) Object.DestroyImmediate(_root.gameObject);
        }

        public void Play(Transform anchor, IFogVisibilityFacade fogVisibility, float scale) =>
            Play(anchor, fogVisibility, scale, Vector3.zero, null);

        /// <summary>
        /// Plays at <paramref name="localOffset"/> in the anchor's space, following it as it moves and turns.
        /// A <paramref name="startColor"/> tints it; null keeps the prefab's colors.
        /// </summary>
        public void Play(Transform anchor, IFogVisibilityFacade fogVisibility, float scale, Vector3 localOffset,
            Color? startColor)
        {
            ExplosionVfx explosion = TakeIdle();
            explosion.transform.localScale = Vector3.one * scale;
            if (startColor.HasValue) explosion.SetStartColor(startColor.Value);
            ActiveExplosion active = new ActiveExplosion(explosion, anchor, fogVisibility, localOffset);
            GeometryUtility.CalculateFrustumPlanes(_camera, _planes);
            active.Follow(_planes);
            explosion.Play();
            _active.Add(active);
            _peakInUse = Mathf.Max(_peakInUse, _active.Count + _fading.Count);
        }

        public void Stop(Transform anchor)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i].Anchor == anchor) FadeAt(i);
            }
        }

        public void Extinguish(Transform anchor, bool immediately)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ActiveExplosion active = _active[i];
                if (active.Anchor != anchor || active.IsExtinguished) continue;
                if (immediately)
                {
                    active.Explosion.Clear();
                    _idle.Add(active.Explosion);
                    _active.RemoveAt(i);
                    continue;
                }

                active.Explosion.StopEmitting();
                _active[i] = active.Extinguish();
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
            if (_active.Count > 0) GeometryUtility.CalculateFrustumPlanes(_camera, _planes);
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

                if ((active.IsExtinguished || !active.Explosion.HasSustainedEmission) && !active.Explosion.IsPlaying)
                {
                    _idle.Add(active.Explosion);
                    _active.RemoveAt(i);
                    continue;
                }

                active.Follow(_planes);
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
                _grownCount++;
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
            private readonly Vector3 _localOffset;

            public ExplosionVfx Explosion { get; }

            public ActiveExplosion(ExplosionVfx explosion, Transform anchor, IFogVisibilityFacade fogVisibility,
                Vector3 localOffset, bool isExtinguished = false)
            {
                Explosion = explosion;
                _anchor = anchor;
                _fogVisibility = fogVisibility;
                _localOffset = localOffset;
                IsExtinguished = isExtinguished;
            }

            public Transform Anchor => _anchor;

            /// <summary>Stopped on a healed unit: still follows the anchor, never resumes emitting.</summary>
            public bool IsExtinguished { get; }

            public ActiveExplosion Extinguish() =>
                new ActiveExplosion(Explosion, _anchor, _fogVisibility, _localOffset, true);

            public void Follow(Plane[] planes)
            {
                Vector3 position = _localOffset == Vector3.zero ? _anchor.position : _anchor.TransformPoint(_localOffset);
                Explosion.transform.position = position;
                bool isHidden = _fogVisibility != null && _fogVisibility.IsHiddenByFog;
                IReadOnlyList<Renderer> renderers = Explosion.Renderers;
                bool visible = !isHidden && GeometryUtility.TestPlanesAABB(planes, new Bounds(position, Vector3.zero));
                for (int i = 0; i < renderers.Count; i++)
                {
                    Renderer renderer = renderers[i];
                    renderer.forceRenderingOff = isHidden;
                    if (!isHidden && !visible) visible = GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
                }
                if (!IsExtinguished) Explosion.SetSustainedEmission(visible);
            }
        }
    }
}
