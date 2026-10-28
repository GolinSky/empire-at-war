using System;
using System.Reflection;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using Object = UnityEngine.Object;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.ViewComponents.Weapon;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Weapon
{
    [Category("Combat")]
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
                actualParticles.GetComponent<ParticleSystemRenderer>().BakeMesh(actualMesh, camera, ParticleSystemBakeMeshOptions.BakeRotationAndScale);
                referenceParticles.GetComponent<ParticleSystemRenderer>().BakeMesh(referenceMesh, camera, ParticleSystemBakeMeshOptions.BakeRotationAndScale);
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

        [TestCase(false)]
        [TestCase(true)]
        public void FlightBatch_TargetDeathOrShieldReturnDoesNotReverseBolt(bool targetDies)
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
                VisualHealth health = new VisualHealth();
                BoltShot shot = bolt.GetComponent<BoltShot>();
                shot.PrepareImpact(new ImpactEffectPresenter(new SilentImpactView(), LoadDamageMatrix()),
                    health, DamageType.Turbolaser, 1f, 1f, true);
                Fire(bolt, muzzle.transform, target.transform, profile, flights);
                float launch = Time.time;
                flights.Advance(launch + 0.6f, 0f);
                Assert.That(bolt.transform.position.z, Is.EqualTo(60f).Within(0.01f));
                if (targetDies)
                {
                    health.IsDestroyed = true;
                    target.transform.position = Vector3.back * 100f;
                }
                else health.HasShields = true;
                flights.Advance(launch + 0.7f, 0f);
                flights.Advance(launch + 0.8f, 0f);
                Assert.That(bolt.transform.position.z, Is.GreaterThanOrEqualTo(60f));
                flights.Advance(launch + 1.1f, 0f);
                Assert.That(bolt.transform.position.z, Is.GreaterThanOrEqualTo(60f));
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
        public void Beam_ImpactChangesFromShieldToArmorAndBackWhileHolding()
        {
            GameObject root = new GameObject("Beam");
            GameObject target = new GameObject("Target");
            GameObject particles = new GameObject("Impact", typeof(ParticleSystem));
            try
            {
                BeamShot shot = root.AddComponent<BeamShot>();
                LineRenderer line = root.AddComponent<LineRenderer>();
                ParticleSystem hit = particles.GetComponent<ParticleSystem>();
                typeof(BeamShot).GetField("beam", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(shot, line);
                typeof(BeamShot).GetField("hitEffect", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(shot, hit);
                VisualHealth health = new VisualHealth { HasShields = true };
                shot.PrepareImpact(new ImpactEffectPresenter(new SilentImpactView(), LoadDamageMatrix()),
                    health, DamageType.Turbolaser, 1f, 1f, true);
                target.transform.position = Vector3.forward * 100f;
                shot.PlayBeam(root.transform, target.transform, 10f, new WeaponProfile());
                typeof(BeamShot).GetField("_startTime", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(shot, Time.time - 1f);
                MethodInfo update = typeof(BeamShot).GetMethod("UpdateBeam", BindingFlags.Instance | BindingFlags.NonPublic);
                update.Invoke(shot, null);
                Assert.That(hit.isPlaying, Is.False);
                health.HasShields = false;
                update.Invoke(shot, null);
                Assert.That(hit.isPlaying, Is.True);
                health.HasShields = true;
                update.Invoke(shot, null);
                Assert.That(hit.isPlaying, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(particles);
            }
        }

        private static DamageMatrixData LoadDamageMatrix() => AssetDatabase.LoadAssetAtPath<DamageMatrixData>(
            "Assets/Settings/Data/Models/Weapon/DamageMatrixData.asset");

        private sealed class SilentImpactView : IImpactEffectView
        {
            public void Emit(ImpactSurface surface, Vector3 position, Vector3 direction, float size) { }
        }

        private sealed class VisualHealth : IHealthModelObserver, IShieldTarget
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
            public ShipClass ShipClass => ShipClass.Capital;
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 100f;
            public float HullPercentage => 1f;
            public float Shields => HasShields ? 100f : 0f;
            public float ShieldPercentage => HasShields ? 1f : 0f;
            public bool IsDestroyed { get; set; }
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public bool HasLiveHardPoints => true;
            public bool HasShields { get; set; }
            public PlayerId Owner => default;
            public IHardPointModel[] GetShipUnits(HardPointType type) => Array.Empty<IHardPointModel>();
            public Vector3 GetImpactPosition(Vector3 origin, Vector3 target, DamageType type) =>
                HasShields ? new Vector3(0f, 0f, Mathf.Max(origin.z, 40f)) : target;
            public bool ShowShieldImpact(Vector3 position, float damage) => true;
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
                        weapons.GetCaliber(caliber), weapons.GetMount(WeaponMount.Single));
                    var particles = Fire(bolt, muzzle.transform, target.transform, profile, flights);
                    particles.GetComponent<ParticleSystemRenderer>().BakeMesh(mesh, camera, ParticleSystemBakeMeshOptions.BakeRotationAndScale);
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
