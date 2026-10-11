using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.ViewComponents.Weapon;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Weapon
{
    public sealed class BoltParticleSimulationTests
    {
        [TestCase("Projectile", 1)]
        [TestCase("DualProjectile", 2)]
        [TestCase("QuadProjectile", 4)]
        [TestCase("OctupleProjectile", 8)]
        [TestCase("HypervelocitySlugShot", 1)]
        public void Fire_PausesStaticBurstAndPreservesRenderedMesh(string name, int count)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Prefabs/Vfx/Projectiles/{name}.prefab");
            GameObject actual = Object.Instantiate(prefab);
            GameObject reference = Object.Instantiate(prefab);
            GameObject muzzle = new GameObject("Test muzzle");
            GameObject target = new GameObject("Test target");
            GameObject cameraObject = new GameObject("Test camera", typeof(UnityEngine.Camera));
            Mesh actualMesh = new Mesh();
            Mesh referenceMesh = new Mesh();
            try
            {
                target.transform.position = Vector3.forward * 100f;
                cameraObject.transform.position = new Vector3(20f, 25f, -30f);
                cameraObject.transform.LookAt(Vector3.zero);
                UnityEngine.Camera camera = cameraObject.GetComponent<UnityEngine.Camera>();
                camera.enabled = false;
                WeaponProfile profile = new WeaponProfile();
                JsonUtility.FromJsonOverwrite("{\"projectileSpeed\":100}", profile);
                ParticleSystem actualParticles = Fire(actual, muzzle.transform, target.transform, profile);
                ParticleSystem referenceParticles = Fire(reference, muzzle.transform, target.transform, profile);

                Assert.That(actualParticles.isPaused, Is.True);
                Assert.That(actualParticles.particleCount, Is.EqualTo(count));
                Assert.That(actual.GetComponentsInChildren<ParticleSystem>(true).Length, Is.EqualTo(1));
                Assert.That(actualParticles.emission.burstCount, Is.EqualTo(1));
                Assert.That(actualParticles.emission.GetBurst(0).time, Is.Zero);
                Assert.That(actualParticles.emission.GetBurst(0).cycleCount, Is.EqualTo(1));
                Assert.That(actualParticles.emission.GetBurst(0).probability, Is.EqualTo(1f));
                Assert.That(actualParticles.emission.rateOverTimeMultiplier, Is.Zero);
                Assert.That(actualParticles.emission.rateOverDistanceMultiplier, Is.Zero);
                Assert.That(actualParticles.main.gravityModifierMultiplier, Is.Zero);
                Assert.That(actualParticles.colorOverLifetime.enabled || actualParticles.colorBySpeed.enabled ||
                    actualParticles.sizeOverLifetime.enabled || actualParticles.sizeBySpeed.enabled ||
                    actualParticles.rotationOverLifetime.enabled || actualParticles.rotationBySpeed.enabled ||
                    actualParticles.velocityOverLifetime.enabled || actualParticles.forceOverLifetime.enabled ||
                    actualParticles.limitVelocityOverLifetime.enabled || actualParticles.inheritVelocity.enabled ||
                    actualParticles.externalForces.enabled || actualParticles.noise.enabled ||
                    actualParticles.collision.enabled || actualParticles.trails.enabled ||
                    actualParticles.textureSheetAnimation.enabled || actualParticles.subEmitters.enabled ||
                    actualParticles.lights.enabled, Is.False, "Bolt visuals must remain a static burst.");

                // Compare against normal simulation later in flight, after the same transform movement.
                referenceParticles.Simulate(0.25f, true, true, false);
                actual.transform.SetPositionAndRotation(new Vector3(3f, 4f, 5f), Quaternion.Euler(10f, 30f, 0f));
                reference.transform.SetPositionAndRotation(actual.transform.position, actual.transform.rotation);
                actualParticles.GetComponent<ParticleSystemRenderer>().BakeMesh(actualMesh, camera, true);
                referenceParticles.GetComponent<ParticleSystemRenderer>().BakeMesh(referenceMesh, camera, true);
                Assert.That(actualMesh.vertexCount, Is.GreaterThan(0));
                Assert.That(actualMesh.vertexCount, Is.EqualTo(referenceMesh.vertexCount));
                CollectionAssert.AreEqual(referenceMesh.colors32, actualMesh.colors32);
                CollectionAssert.AreEqual(referenceMesh.uv, actualMesh.uv);
                Vector3[] actualVertices = actualMesh.vertices;
                Vector3[] referenceVertices = referenceMesh.vertices;
                for (int i = 0; i < actualVertices.Length; i++)
                    Assert.That(Vector3.Distance(actualVertices[i], referenceVertices[i]), Is.LessThan(0.001f));

                actual.GetComponent<BoltShot>().ResetForPool();
                Assert.That(actualParticles.particleCount, Is.Zero);
                Assert.That(actualParticles.isStopped, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(actual);
                Object.DestroyImmediate(reference);
                Object.DestroyImmediate(muzzle);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(actualMesh);
                Object.DestroyImmediate(referenceMesh);
            }
        }

        private static ParticleSystem Fire(GameObject root, Transform muzzle, Transform target, WeaponProfile profile)
        {
            BoltShot shot = root.GetComponent<BoltShot>();
            ParticleSystem particles = (ParticleSystem)typeof(BoltShot)
                .GetField("vfx", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(shot);
            particles.useAutoRandomSeed = false;
            particles.randomSeed = 12345;
            shot.Fire(muzzle, target, target, Vector3.zero, profile);
            return particles;
        }
    }
}
