using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Timing;
using Unity.Jobs;
using UnityEngine;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>
    /// Flies and draws every bolt of the battle with no object per bolt. Each <see cref="BoltShot"/> prefab gets one
    /// <see cref="BoltLayer"/>: a shared copy of its particle system that shows all its bolts, so bolts never run out.
    /// Tracking and landing run in Tick on the main thread, since impacts need managed state. PostTick (after every
    /// Update, so bolts fired this frame are included) runs one Burst job per layer and hands the particles to Unity
    /// before it simulates and bounds particle systems for rendering.
    /// </summary>
    [LifecycleOrder(Dispose = LifecycleOrders.BOLT_FLIGHT_DISPOSE)]
    public sealed class BoltFlightBatch : ITickable, IPostTickable, IOrderedDisposable
    {
        private readonly Dictionary<BoltShot, BoltLayer> _layers = new Dictionary<BoltShot, BoltLayer>();
        private readonly List<BoltLayer> _layerList = new List<BoltLayer>();
        private readonly Stack<BoltFlight> _free = new Stack<BoltFlight>();
        private Transform _root;

        public int Count
        {
            get
            {
                int count = 0;
                foreach (BoltLayer layer in _layerList) count += layer.Flights.Count;
                return count;
            }
        }

        /// <summary>Builds the shared drawing for a bolt prefab ahead of its first shot (loading, unit spawn).</summary>
        public void Prepare(BoltShot prefab) => GetLayer(prefab);

        /// <returns>Seconds until the bolt lands.</returns>
        public float Launch(BoltShot prefab, WeaponProfile profile, Transform muzzle, Transform target,
            Transform targetPivot, Vector3 aimOffset, IHealthModelObserver targetHealth, ImpactEffectPresenter impacts,
            float impactDamage, float impactSize, bool isHit, IncomingMissile missile, float lateSeconds)
        {
            BoltLayer layer = GetLayer(prefab);
            BoltFlight flight = _free.Count > 0 ? _free.Pop() : new BoltFlight();
            Vector3 muzzlePosition = muzzle.position;
            float remaining = flight.Launch(prefab, profile, muzzlePosition, target, targetPivot, aimOffset,
                targetHealth, impacts, impactDamage, impactSize, isHit, missile, lateSeconds);
            layer.PlayFlash(muzzle, flight.Pose.Start + flight.Pose.Path, profile.Color, profile.Size.x);
            layer.Flights.Add(flight);
            return remaining;
        }

        private BoltLayer GetLayer(BoltShot prefab)
        {
            if (_layers.TryGetValue(prefab, out BoltLayer layer)) return layer;
            if (_root == null) _root = new GameObject(nameof(BoltFlightBatch)).transform;
            layer = new BoltLayer(prefab, _root);
            _layers.Add(prefab, layer);
            _layerList.Add(layer);
            return layer;
        }

        public void Tick()
        {
            AdvanceFlights(Time.time, Time.deltaTime);
            // Shots that fired without a muzzle flash grow the flash stock by one per frame.
            foreach (BoltLayer layer in _layerList)
                if (layer.GrowFlashes()) break;
        }

        public void PostTick() => DrawParticles();

        /// <summary>Flies every bolt to <paramref name="now"/> and draws the result, as one frame does.</summary>
        public void Advance(float now, float deltaTime)
        {
            AdvanceFlights(now, deltaTime);
            DrawParticles();
        }

        internal void AdvanceFlights(float now, float deltaTime)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.ProjectileTurretUpdate.Auto())
            using (BattleProfilerMarkers.ProjectileTrack.Auto())
#endif
            {
                foreach (BoltLayer layer in _layerList)
                {
                    List<BoltFlight> flights = layer.Flights;
                    for (int i = flights.Count - 1; i >= 0; i--)
                    {
                        BoltFlight flight = flights[i];
                        if (flight.Advance(now, deltaTime)) continue;
                        int last = flights.Count - 1;
                        flights[i] = flights[last];
                        flights.RemoveAt(last);
                        _free.Push(flight);
                    }
                    layer.TickFlashes();
                }
            }
        }

        /// <summary>Writes every flying bolt into its layer's particles; one Burst job per layer.</summary>
        internal void DrawParticles()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.ProjectileMove.Auto())
#endif
            {
                JobHandle placement = default;
                foreach (BoltLayer layer in _layerList)
                    placement = JobHandle.CombineDependencies(placement, layer.ScheduleParticles());
                placement.Complete();
                foreach (BoltLayer layer in _layerList) layer.ApplyParticles();
            }
        }

        public void Dispose()
        {
            foreach (BoltLayer layer in _layerList) layer.Dispose();
            _layers.Clear();
            _layerList.Clear();
            // Scene teardown may already have destroyed the root with the scene.
            if (_root == null) return;
            if (Application.isPlaying) Object.Destroy(_root.gameObject);
            else Object.DestroyImmediate(_root.gameObject);
        }

        /// <summary>Drops every bolt in flight without impacts; layers stay ready for the next battle.</summary>
        public void Clear()
        {
            foreach (BoltLayer layer in _layerList)
            {
                foreach (BoltFlight flight in layer.Flights) _free.Push(flight);
                layer.Clear();
            }
        }
    }
}
