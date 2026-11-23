using System;
using System.Collections.Generic;
using EmpireAtWar.Utils;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>
    /// Every flying bolt of one <see cref="BoltShot"/> prefab, drawn as particles of one shared copy of the prefab's
    /// particle system, plus that prefab's pooled muzzle flashes. A shot with no idle flash fires without one.
    /// </summary>
    internal sealed class BoltLayer : IDisposable
    {
        private const int MAX_PARTICLES = 200000;
        private const int FLASH_PREWARM = 16;
        private const int MAX_FLASHES = 256;
        private const float PARTICLE_BURST_TIME = 0.001f;

        private readonly BoltShot _prefab;
        private readonly Transform _root;
        private readonly BoltShot _host;
        private readonly ParticleSystem _particles;
        private readonly Stack<MuzzleFlashView> _idleFlashes = new Stack<MuzzleFlashView>();
        private readonly List<MuzzleFlashView> _activeFlashes = new List<MuzzleFlashView>();
        private NativeArray<float3> _offsets;
        private NativeArray<BoltPose> _poses;
        private NativeArray<BoltLook> _looks;
        private NativeArray<ParticleSystem.Particle> _buffer;
        private int _flashCount;
        private int _flashDemand;
        private int _scheduledCount;
        private int _drawnCount;

        internal readonly List<BoltFlight> Flights = new List<BoltFlight>();

        internal int Barrels => _offsets.Length;
        internal int FlashCount => _flashCount;
        internal int DrawnParticles => _drawnCount * Barrels;

        internal BoltLayer(BoltShot prefab, Transform root)
        {
            _prefab = prefab;
            _root = root;
            _host = Object.Instantiate(prefab, root);
            _host.enabled = false;
            _host.name = prefab.name + "Bolts";
            _host.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _particles = _host.Vfx;
            _offsets = ReadBarrelOffsets(_particles);
            ConfigureForSharedDrawing(_particles);
            for (int i = 0; i < FLASH_PREWARM; i++) CreateFlash();
        }

        // The authored burst places one particle per barrel; its local positions are the barrel offsets.
        private static NativeArray<float3> ReadBarrelOffsets(ParticleSystem particles)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.startSpeed = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = true;
            particles.Simulate(PARTICLE_BURST_TIME, true, true, false);
            var burst = new ParticleSystem.Particle[particles.particleCount];
            int count = particles.GetParticles(burst);
            if (count == 0) throw new InvalidOperationException($"{particles.name}: the bolt burst emits no particles.");
            var offsets = new NativeArray<float3>(count, Allocator.Persistent);
            for (int i = 0; i < count; i++) offsets[i] = burst[i].position;
            particles.Clear(true);
            return offsets;
        }

        private static void ConfigureForSharedDrawing(ParticleSystem particles)
        {
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.maxParticles = MAX_PARTICLES;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = false;
            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = false;
            particles.Play(true);
        }

        internal void PlayFlash(Transform muzzle, Vector3 aimPoint, Color color, float size)
        {
            if (_idleFlashes.Count == 0)
            {
                if (_flashCount + _flashDemand < MAX_FLASHES) _flashDemand++;
                return;
            }
            MuzzleFlashView flash = _idleFlashes.Pop();
            flash.Play(muzzle, aimPoint, color, size);
            _activeFlashes.Add(flash);
        }

        internal void TickFlashes()
        {
            for (int i = _activeFlashes.Count - 1; i >= 0; i--)
            {
                MuzzleFlashView flash = _activeFlashes[i];
                flash.Tick();
                if (flash.IsPlaying) continue;
                _activeFlashes[i] = _activeFlashes[_activeFlashes.Count - 1];
                _activeFlashes.RemoveAt(_activeFlashes.Count - 1);
                _idleFlashes.Push(flash);
            }
        }

        /// <summary>Adds one flash for shots that fired without one; never more than one per call.</summary>
        internal bool GrowFlashes()
        {
            if (_flashDemand == 0) return false;
            _flashDemand--;
            CreateFlash();
            return true;
        }

        private void CreateFlash()
        {
            MuzzleFlashView flash = Object.Instantiate(_prefab.MuzzleFlash, _root);
            flash.Stop();
            _idleFlashes.Push(flash);
            _flashCount++;
        }

        internal JobHandle ScheduleParticles()
        {
            _scheduledCount = Flights.Count;
            if (_scheduledCount == 0) return default;
            NativeArrayBuffer.EnsureCapacity(ref _poses, _scheduledCount);
            NativeArrayBuffer.EnsureCapacity(ref _looks, _scheduledCount);
            NativeArrayBuffer.EnsureCapacity(ref _buffer, _scheduledCount * Barrels);
            for (int i = 0; i < _scheduledCount; i++)
            {
                BoltFlight flight = Flights[i];
                _poses[i] = flight.Pose;
                _looks[i] = new BoltLook { Size = flight.Size, Color = flight.Color };
            }
            return new BoltParticleJob
            {
                Poses = _poses,
                Looks = _looks,
                Offsets = _offsets,
                Particles = _buffer
            }.Schedule(_scheduledCount, 64);
        }

        /// <summary>Hands the written particles to the shared system; call after the job completed.</summary>
        internal void ApplyParticles()
        {
            if (_scheduledCount == 0 && _drawnCount == 0) return;
            if (_scheduledCount == 0) _particles.Clear(false);
            else _particles.SetParticles(_buffer, _scheduledCount * Barrels);
            _drawnCount = _scheduledCount;
        }

        internal void Clear()
        {
            foreach (BoltFlight flight in Flights) flight.Stop();
            Flights.Clear();
            _scheduledCount = 0;
            _drawnCount = 0;
            if (_particles != null) _particles.Clear(false);
            foreach (MuzzleFlashView flash in _activeFlashes)
            {
                if (flash == null) continue;
                flash.Stop();
                _idleFlashes.Push(flash);
            }
            _activeFlashes.Clear();
            _flashDemand = 0;
        }

        public void Dispose()
        {
            Clear();
            if (_offsets.IsCreated) _offsets.Dispose();
            if (_poses.IsCreated) _poses.Dispose();
            if (_looks.IsCreated) _looks.Dispose();
            if (_buffer.IsCreated) _buffer.Dispose();
        }
    }
}
