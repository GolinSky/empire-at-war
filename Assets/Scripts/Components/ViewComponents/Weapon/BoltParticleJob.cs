using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>
    /// Writes one bolt's barrel particles: index = bolt in its <see cref="BoltFlightBatch"/> layer, and each barrel
    /// sits at its authored offset in the bolt's facing. The tiny velocity only gives the stretched billboard its axis.
    /// </summary>
    [BurstCompile]
    internal struct BoltParticleJob : IJobParallelFor
    {
        private const float STRETCH_AXIS_SPEED = 0.001f;
        // Particles are rewritten every frame; the lifetime only has to outlast one frame of simulation.
        private const float LIFETIME = 1000f;

        [ReadOnly] public NativeArray<BoltPose> Poses;
        [ReadOnly] public NativeArray<BoltLook> Looks;
        [ReadOnly] public NativeArray<float3> Offsets;
        [NativeDisableParallelForRestriction] public NativeArray<ParticleSystem.Particle> Particles;

        public void Execute(int index)
        {
            BoltPose pose = Poses[index];
            BoltLook look = Looks[index];
            float3 position = pose.Start + pose.Path * pose.Progress;
            float3 forward = math.normalizesafe(pose.Path, new float3(0f, 0f, 1f));
            quaternion rotation = quaternion.LookRotationSafe(forward, math.up());
            int first = index * Offsets.Length;
            for (int barrel = 0; barrel < Offsets.Length; barrel++)
            {
                ParticleSystem.Particle particle = default;
                particle.position = position + math.rotate(rotation, Offsets[barrel]);
                particle.velocity = forward * STRETCH_AXIS_SPEED;
                particle.startSize3D = look.Size;
                particle.startColor = look.Color;
                particle.startLifetime = LIFETIME;
                particle.remainingLifetime = LIFETIME;
                Particles[first + barrel] = particle;
            }
        }
    }
}
