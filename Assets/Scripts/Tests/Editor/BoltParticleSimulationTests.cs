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
            BoltFlightBatch flights = new BoltFlightBatch();
            try
            {
                target.transform.position = Vector3.forward * 100f;
                cameraObject.transform.position = new Vector3(20f, 25f, -30f);
                cameraObject.transform.LookAt(Vector3.zero);
                UnityEngine.Camera camera = cameraObject.GetComponent<UnityEngine.Camera>();
                camera.enabled = false;
                WeaponProfile profile = new WeaponProfile();
                JsonUtility.FromJsonOverwrite("{\"projectileSpeed\":100}", profile);
                ParticleSystem actualParticles = Fire(actual, muzzle.transform, target.transform, profile, flights);
                ParticleSystem referenceParticles = Fire(reference, muzzle.transform, target.transform, profile, flights);

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
                flights.Dispose();
                Object.DestroyImmediate(actual);
                Object.DestroyImmediate(reference);
                Object.DestroyImmediate(muzzle);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(actualMesh);
                Object.DestroyImmediate(referenceMesh);
            }
        }

        [Test]
        public void FlightBatch_PlacesBoltOnItsPathThenDropsItOnArrival()
        {
            GameObject bolt = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Vfx/Projectiles/Projectile.prefab"));
            GameObject muzzle = new GameObject("Test muzzle");
            GameObject target = new GameObject("Test target");
            BoltFlightBatch flights = new BoltFlightBatch();
            try
            {
                target.transform.position = Vector3.forward * 100f;
                WeaponProfile profile = new WeaponProfile();
                JsonUtility.FromJsonOverwrite("{\"projectileSpeed\":100}", profile);
                Fire(bolt, muzzle.transform, target.transform, profile, flights);
                float launch = Time.time;

                flights.Advance(launch + 0.5f, 0f);

                Assert.That(flights.Count, Is.EqualTo(1));
                Assert.That(Vector3.Distance(bolt.transform.position, Vector3.forward * 50f), Is.LessThan(0.01f));
                Assert.That(Vector3.Angle(bolt.transform.forward, Vector3.forward), Is.LessThan(0.01f));

                flights.Advance(launch + 2f, 0f);

                Assert.That(flights.Count, Is.Zero);
                Assert.That(bolt.transform.position, Is.EqualTo(target.transform.position));
            }
            finally
            {
                flights.Dispose();
                Object.DestroyImmediate(bolt);
                Object.DestroyImmediate(muzzle);
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void FlightBatch_AimJumpAtSurfaceClipBendsFlightInsteadOfTeleporting()
        {
            GameObject bolt = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Vfx/Projectiles/Projectile.prefab"));
            GameObject muzzle = new GameObject("Test muzzle");
            GameObject target = new GameObject("Test target");
            BoltFlightBatch flights = new BoltFlightBatch();
            try
            {
                target.transform.position = Vector3.forward * 100f;
                WeaponProfile profile = new WeaponProfile();
                JsonUtility.FromJsonOverwrite("{\"projectileSpeed\":100}", profile);
                Fire(bolt, muzzle.transform, target.transform, profile, flights);
                float launch = Time.time;
                flights.Advance(launch + 0.5f, 0f);

                // Aim jumps sideways (e.g. the target's shield collapses); the next advance re-clips the aim.
                target.transform.position = new Vector3(40f, 0f, 100f);
                flights.Advance(launch + 0.6f, 0f);

                Assert.That(Vector3.Distance(bolt.transform.position, Vector3.forward * 60f), Is.LessThan(0.01f));

                flights.Advance(launch + 0.8f, 0f);

                Assert.That(Vector3.Distance(bolt.transform.position, new Vector3(20f, 0f, 80f)), Is.LessThan(0.01f));

                flights.Advance(launch + 2f, 0f);

                Assert.That(bolt.transform.position, Is.EqualTo(target.transform.position));
            }
            finally
            {
                flights.Dispose();
                Object.DestroyImmediate(bolt);
                Object.DestroyImmediate(muzzle);
                Object.DestroyImmediate(target);
            }
        }

        [TestCase(WeaponFamily.Laser)]
        [TestCase(WeaponFamily.Ion)]
        [TestCase(WeaponFamily.Turbolaser)]
        [TestCase(WeaponFamily.FighterLaser)]
        [TestCase(WeaponFamily.FighterIon)]
        public void Caliber_ChangesRenderedLengthOnly_AndPreservesColor(WeaponFamily family)
        {
            var weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(
                "Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            var rule = weapons.GetFamily(family);
            GameObject bolt = Object.Instantiate(rule.ShotPrefab.gameObject);
            GameObject muzzle = new GameObject("Test muzzle");
            GameObject target = new GameObject("Test target");
            GameObject cameraObject = new GameObject("Test camera", typeof(UnityEngine.Camera));
            Mesh mesh = new Mesh();
            BoltFlightBatch flights = new BoltFlightBatch();
            try
            {
                target.transform.position = Vector3.forward * 100f;
                cameraObject.transform.position = Vector3.right * 20f;
                cameraObject.transform.LookAt(Vector3.zero);
                var camera = cameraObject.GetComponent<UnityEngine.Camera>();
                camera.enabled = false;
                float width = 0f;
                float lightLength = 0f;
                foreach (WeaponCaliber caliber in new[] { WeaponCaliber.Light, WeaponCaliber.Medium, WeaponCaliber.Heavy })
                {
                    Object.DestroyImmediate(bolt);
                    bolt = Object.Instantiate(rule.ShotPrefab.gameObject);
                    var profile = WeaponProfileFactory.Create(new WeaponDefinition(), rule,
                        weapons.GetCaliber(caliber), weapons.GetRangeClass(WeaponRangeClass.Standard));
                    var particles = Fire(bolt, muzzle.transform, target.transform, profile, flights);
                    particles.GetComponent<ParticleSystemRenderer>().BakeMesh(mesh, camera, true);
                    mesh.RecalculateBounds();
                    Assert.That(mesh.vertexCount, Is.GreaterThan(0));
                    Assert.That(particles.main.startColor.color, Is.EqualTo(rule.Color));
                    if (caliber == WeaponCaliber.Light)
                    {
                        width = mesh.bounds.size.y;
                        lightLength = mesh.bounds.size.z;
                        Assert.That(width, Is.GreaterThan(0f));
                        Assert.That(lightLength, Is.GreaterThan(0f));
                    }
                    else
                    {
                        Assert.That(mesh.bounds.size.y, Is.EqualTo(width).Within(0.001f));
                        float ratio = caliber == WeaponCaliber.Medium ? 1f / 0.6f : 1.6f / 0.6f;
                        Assert.That(mesh.bounds.size.z, Is.EqualTo(lightLength * ratio).Within(0.001f));
                    }
                    bolt.GetComponent<BoltShot>().ResetForPool();
                }
                float expectedWidth = family == WeaponFamily.Turbolaser ? 3f :
                    family == WeaponFamily.FighterLaser || family == WeaponFamily.FighterIon ? 0.6f : 1.05f;
                Assert.That(width, Is.EqualTo(expectedWidth).Within(0.001f));
                Assert.That(lightLength, Is.EqualTo(rule.Size.y * 0.3f * 3f * 2f).Within(0.001f));
            }
            finally
            {
                flights.Dispose();
                Object.DestroyImmediate(bolt);
                Object.DestroyImmediate(muzzle);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(mesh);
            }
        }

        private static ParticleSystem Fire(GameObject root, Transform muzzle, Transform target, WeaponProfile profile,
            BoltFlightBatch flights)
        {
            BoltShot shot = root.GetComponent<BoltShot>();
            ParticleSystem particles = (ParticleSystem)typeof(BoltShot)
                .GetField("vfx", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(shot);
            particles.useAutoRandomSeed = false;
            particles.randomSeed = 12345;
            shot.Fire(muzzle, target, target, Vector3.zero, profile, flights);
            return particles;
        }
    }
}
