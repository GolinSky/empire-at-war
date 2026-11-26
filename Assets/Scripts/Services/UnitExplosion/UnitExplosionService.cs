using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Pooling;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Services.UnitExplosion
{
    public sealed class UnitExplosionService : Service, IUnitExplosionService, ITickable, IOrderedDisposable
    {
        private const float COVERAGE_PADDING = 1.2f;
        // An orbital hit the unit survives stays a local blast: at most a Star Destroyer's hull, not a dreadnought's.
        private const float MAX_ORBITAL_HIT_DIAMETER = 240f;

        private const int MAX_IDLE_EXPLOSIONS = 32;
        private const int MAX_IDLE_SHOCKWAVES = 16;
        // Each shockwave draws a screen-covering refraction; past this many at once, new deaths skip theirs.
        private const int MAX_ACTIVE_SHOCKWAVES = 8;
        // Largest share of the camera distance an explosion moves towards the camera.
        private const float MAX_CAMERA_SHARE = 0.5f;

        private readonly UnitExplosionView _destructionPrefab;
        private readonly UnitExplosionView _orbitalHitPrefab;
        private readonly ViewPool<UnitExplosionView> _destructionPool;
        private readonly ViewPool<UnitExplosionView> _orbitalHitPool;
        private readonly ViewPool<ShockwaveView> _shockwavePool;
        private readonly List<ActiveExplosion> _active = new List<ActiveExplosion>();
        private readonly List<ShockwaveView> _activeShockwaves = new List<ShockwaveView>();
        private readonly Plane[] _planes = new Plane[6];
        private readonly IVisionService _vision;
        private readonly ILocalPlayer _localPlayer;
        private int _peakDestruction;
        private int _peakOrbitalHit;

        private readonly UnityEngine.Camera _camera;

        public UnitExplosionService(UnitExplosionView destructionPrefab, UnitExplosionView orbitalHitPrefab,
            ShockwaveView shockwavePrefab, UnityEngine.Camera camera, IVisionService vision, ILocalPlayer localPlayer)
        {
            _camera = camera;
            _vision = vision;
            _localPlayer = localPlayer;
            _destructionPrefab = destructionPrefab;
            _orbitalHitPrefab = orbitalHitPrefab;
            _destructionPool = new ViewPool<UnitExplosionView>(prefab: destructionPrefab, rootName: "UnitExplosions",
                hide: view => view.Hide(), maxSize: MAX_IDLE_EXPLOSIONS);
            _orbitalHitPool = new ViewPool<UnitExplosionView>(prefab: orbitalHitPrefab, rootName: "OrbitalHitExplosions",
                hide: view => view.Hide(), maxSize: MAX_IDLE_EXPLOSIONS);
            _shockwavePool = new ViewPool<ShockwaveView>(prefab: shockwavePrefab, rootName: "UnitShockwaves",
                hide: view => view.Hide(), maxSize: MAX_IDLE_SHOCKWAVES);
        }

        public void Dispose()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Data for sizing these pools: the most explosions of each kind alive at once.
            if (Application.isPlaying)
                Debug.Log($"[VfxPools] {_destructionPrefab.name} peak={_peakDestruction}; {_orbitalHitPrefab.name} peak={_peakOrbitalHit}");
#endif
            Clear();
            _destructionPool.Dispose();
            _orbitalHitPool.Dispose();
            _shockwavePool.Dispose();
        }

        public void Clear()
        {
            foreach (ActiveExplosion active in _active)
            {
                active.Pool.Release(active.View);
            }

            _active.Clear();
            foreach (ShockwaveView shockwave in _activeShockwaves)
            {
                _shockwavePool.Release(shockwave);
            }

            _activeShockwaves.Clear();
        }

        public void Spawn(IReadOnlyList<Renderer> hullRenderers, UnitExplosionKind kind, float coverageScale = 1f)
        {
            Bounds bounds = hullRenderers[0].bounds;
            for (int i = 1; i < hullRenderers.Count; i++)
            {
                bounds.Encapsulate(hullRenderers[i].bounds);
            }

            // The enclosing sphere covers every corner, even for a long or rotated hull.
            float diameter = bounds.extents.magnitude * 2f * COVERAGE_PADDING * coverageScale;
            bool isOrbitalHit = kind == UnitExplosionKind.OrbitalHit;
            if (isOrbitalHit) diameter = Mathf.Min(diameter, MAX_ORBITAL_HIT_DIAMETER);
            ViewPool<UnitExplosionView> pool = isOrbitalHit ? _orbitalHitPool : _destructionPool;
            UnitExplosionView prefab = isOrbitalHit ? _orbitalHitPrefab : _destructionPrefab;
            UnitExplosionView view = pool.Get();
            float radius = diameter * 0.5f;
            view.Play(InFrontOfHull(bounds.center, radius), diameter / prefab.Diameter);
            _active.Add(new ActiveExplosion(view, pool, bounds.center, radius));
            float hullRadius = bounds.extents.magnitude;
            // A death the local player cannot see gets no shockwave, so fog never reveals it and the frame copy stays off.
            if (!isOrbitalHit && _activeShockwaves.Count < MAX_ACTIVE_SHOCKWAVES &&
                _vision.IsAreaVisible(_localPlayer.Id, bounds.center, hullRadius))
            {
                ShockwaveView shockwave = _shockwavePool.Get();
                shockwave.Play(bounds.center, hullRadius);
                GeometryUtility.CalculateFrustumPlanes(_camera, _planes);
                shockwave.SetOnScreen(GeometryUtility.TestPlanesAABB(_planes, shockwave.Bounds));
                _activeShockwaves.Add(shockwave);
            }

            int alive = 0;
            foreach (ActiveExplosion active in _active)
            {
                if (active.Pool == pool) alive++;
            }

            if (isOrbitalHit) _peakOrbitalHit = Mathf.Max(_peakOrbitalHit, alive);
            else _peakDestruction = Mathf.Max(_peakDestruction, alive);
        }

        public void Tick()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ActiveExplosion active = _active[i];
                if (!active.View.IsAlive)
                {
                    active.Pool.Release(active.View);
                    _active.RemoveAt(i);
                    continue;
                }

                active.View.transform.position = InFrontOfHull(active.Center, active.Radius);
            }

            if (_activeShockwaves.Count == 0) return;
            float deltaTime = Time.deltaTime;
            GeometryUtility.CalculateFrustumPlanes(_camera, _planes);
            for (int i = _activeShockwaves.Count - 1; i >= 0; i--)
            {
                ShockwaveView shockwave = _activeShockwaves[i];
                shockwave.Advance(deltaTime);
                if (shockwave.IsAlive)
                {
                    // Off-screen waves keep ageing but no longer make the renderer copy the frame.
                    shockwave.SetOnScreen(GeometryUtility.TestPlanesAABB(_planes, shockwave.Bounds));
                    continue;
                }

                _shockwavePool.Release(shockwave);
                _activeShockwaves.RemoveAt(i);
            }
        }

        // The explosion is one camera-facing billboard. At the hull's center the hull's own depth hides half of it, so
        // it sits on the hull sphere's camera side; ships and planets truly in front still cover it.
        private Vector3 InFrontOfHull(Vector3 center, float radius)
        {
            Vector3 toCamera = _camera.transform.position - center;
            float distance = toCamera.magnitude;
            if (distance <= Mathf.Epsilon) return center;
            // Stay well in front of the camera when it is zoomed inside the hull sphere.
            return center + toCamera * (Mathf.Min(radius, distance * MAX_CAMERA_SHARE) / distance);
        }

        private readonly struct ActiveExplosion
        {
            public ActiveExplosion(UnitExplosionView view, ViewPool<UnitExplosionView> pool, Vector3 center, float radius)
            {
                View = view;
                Pool = pool;
                Center = center;
                Radius = radius;
            }

            public UnitExplosionView View { get; }
            public ViewPool<UnitExplosionView> Pool { get; }
            public Vector3 Center { get; }
            public float Radius { get; }
        }
    }
}
