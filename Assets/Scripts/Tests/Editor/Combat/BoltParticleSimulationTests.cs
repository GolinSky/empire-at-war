using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.ViewComponents.Weapon;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Weapon
{
    [Category("Combat")]
    public sealed class BoltParticleSimulationTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private BoltFlightBatch _flights;
        private Transform _muzzle;
        private Transform _target;
        private BoltTestRig.VisualHealth _health;
        private BoltTestRig.RecordingImpactView _impactView;
        private ImpactEffectPresenter _impacts;

        [SetUp]
        public void SetUp()
        {
            _flights = new BoltFlightBatch();
            _muzzle = Create("Test muzzle").transform;
            _target = Create("Test target").transform;
            _target.position = Vector3.forward * 100f;
            _health = new BoltTestRig.VisualHealth();
            _impactView = new BoltTestRig.RecordingImpactView();
            _impacts = BoltTestRig.Impacts(_impactView);
        }

        [TearDown]
        public void TearDown()
        {
            _flights.Dispose();
            foreach (GameObject instance in _objects) Object.DestroyImmediate(instance);
            _objects.Clear();
        }

        [TestCase("Projectile", 1)]
        [TestCase("DualProjectile", 2)]
        [TestCase("QuadProjectile", 4)]
        [TestCase("OctupleProjectile", 8)]
        [TestCase("HypervelocitySlugShot", 1)]
        public void SharedLayer_DrawsOneParticlePerBarrelAlongEachFlight(string name, int barrels)
        {
            BoltShot prefab = BoltTestRig.LoadBolt(name);
            ParticleSystem authored = prefab.Vfx;
            // The layer reads barrel offsets from one static burst; anything else would draw the wrong shape.
            Assert.That(authored.emission.burstCount, Is.EqualTo(1));
            Assert.That(authored.emission.GetBurst(0).time, Is.Zero);
            Assert.That(authored.emission.GetBurst(0).cycleCount, Is.EqualTo(1));
            Assert.That(authored.emission.GetBurst(0).probability, Is.EqualTo(1f));
            Assert.That(authored.emission.rateOverTimeMultiplier, Is.Zero);
            Assert.That(authored.emission.rateOverDistanceMultiplier, Is.Zero);
            Assert.That(authored.colorOverLifetime.enabled || authored.colorBySpeed.enabled ||
                authored.sizeBySpeed.enabled || authored.rotationOverLifetime.enabled ||
                authored.rotationBySpeed.enabled || authored.velocityOverLifetime.enabled ||
                authored.forceOverLifetime.enabled || authored.limitVelocityOverLifetime.enabled ||
                authored.inheritVelocity.enabled || authored.externalForces.enabled || authored.noise.enabled ||
                authored.collision.enabled || authored.trails.enabled || authored.textureSheetAnimation.enabled ||
                authored.subEmitters.enabled || authored.lights.enabled, Is.False,
                "Bolt visuals must remain a static burst.");

            WeaponProfile profile = BoltTestRig.SpeedProfile(100f);
            float launch = Time.time;
            BoltTestRig.Launch(_flights, prefab, profile, _muzzle, _target, _health, _impacts);
            BoltTestRig.Launch(_flights, prefab, profile, _muzzle, _target, _health, _impacts, lateSeconds: 0.25f);
            _flights.Advance(launch + 0.5f, 0f);

            BoltLayer layer = BoltTestRig.Layer(_flights, prefab);
            Assert.That(layer.Barrels, Is.EqualTo(barrels));
            ParticleSystem shared = BoltTestRig.Particles(layer);
            var particles = new ParticleSystem.Particle[2 * barrels + 1];
            Assert.That(shared.GetParticles(particles), Is.EqualTo(2 * barrels));
            foreach (ParticleSystem.Particle particle in particles.Take(2 * barrels))
                Assert.That(Vector3.Dot(particle.velocity.normalized, Vector3.forward), Is.GreaterThan(0.999f),
                    "Stretched bolts must point along flight.");
            Assert.That(particles.Take(2 * barrels).Select(particle => particle.position).Distinct().Count(),
                Is.EqualTo(2 * barrels));
            Assert.That(shared.main.simulationSpace, Is.EqualTo(ParticleSystemSimulationSpace.World));

            _flights.Advance(launch + 2f, 0f);
            Assert.That(shared.particleCount, Is.Zero, "Landed bolts leave the shared system.");
        }

        [Test]
        public void FlightBatch_PlacesBoltOnItsPathThenLandsOnTarget()
        {
            BoltShot prefab = BoltTestRig.LoadBolt("Projectile");
            float launch = Time.time;
            BoltTestRig.Launch(_flights, prefab, BoltTestRig.SpeedProfile(100f), _muzzle, _target, _health, _impacts,
                isHit: true);

            _flights.Advance(launch + 0.5f, 0f);

            Assert.That(_flights.Count, Is.EqualTo(1));
            BoltFlight flight = Flight(prefab);
            Assert.That(Vector3.Distance(BoltTestRig.Position(flight), Vector3.forward * 50f), Is.LessThan(0.01f));
            Assert.That(Vector3.Angle((Vector3)flight.Pose.Path, Vector3.forward), Is.LessThan(0.01f));

            _flights.Advance(launch + 2f, 0f);

            Assert.That(_flights.Count, Is.Zero);
            Assert.That(_impactView.Positions, Has.Count.EqualTo(1));
            Assert.That(Vector3.Distance(_impactView.Positions[0], _target.position), Is.LessThan(0.01f));
        }

        [Test]
        public void FlightBatch_AimJumpAtSurfaceClipBendsFlightInsteadOfTeleporting()
        {
            BoltShot prefab = BoltTestRig.LoadBolt("Projectile");
            float launch = Time.time;
            BoltTestRig.Launch(_flights, prefab, BoltTestRig.SpeedProfile(100f), _muzzle, _target, _health, _impacts,
                isHit: true);
            _flights.Advance(launch + 0.5f, 0f);

            // Aim jumps sideways (e.g. the target's shield collapses); the next advance re-clips the aim.
            _target.position = new Vector3(40f, 0f, 100f);
            _flights.Advance(launch + 0.6f, 0f);

            Assert.That(Vector3.Distance(BoltTestRig.Position(Flight(prefab)), Vector3.forward * 60f), Is.LessThan(0.01f));

            _flights.Advance(launch + 0.8f, 0f);

            Assert.That(Vector3.Distance(BoltTestRig.Position(Flight(prefab)), new Vector3(20f, 0f, 80f)),
                Is.LessThan(0.01f));

            _flights.Advance(launch + 2f, 0f);

            Assert.That(Vector3.Distance(_impactView.Positions[0], _target.position), Is.LessThan(0.01f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FlightBatch_TargetDeathOrShieldReturnDoesNotReverseBolt(bool targetDies)
        {
            BoltShot prefab = BoltTestRig.LoadBolt("Projectile");
            float launch = Time.time;
            BoltTestRig.Launch(_flights, prefab, BoltTestRig.SpeedProfile(100f), _muzzle, _target, _health, _impacts,
                isHit: true);
            _flights.Advance(launch + 0.6f, 0f);
            Assert.That(BoltTestRig.Position(Flight(prefab)).z, Is.EqualTo(60f).Within(0.01f));
            if (targetDies)
            {
                _health.IsDestroyed = true;
                _target.position = Vector3.back * 100f;
            }
            else _health.HasShields = true;
            _flights.Advance(launch + 0.7f, 0f);
            _flights.Advance(launch + 0.8f, 0f);
            Assert.That(BoltTestRig.Position(Flight(prefab)).z, Is.GreaterThanOrEqualTo(60f));
            _flights.Advance(launch + 0.95f, 0f);
            Assert.That(BoltTestRig.Position(Flight(prefab)).z, Is.GreaterThanOrEqualTo(60f));
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
                var health = new BoltTestRig.VisualHealth { HasShields = true };
                shot.PrepareImpact(BoltTestRig.Impacts(new BoltTestRig.RecordingImpactView()),
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

        [TestCase(WeaponFamily.Laser)]
        [TestCase(WeaponFamily.Ion)]
        [TestCase(WeaponFamily.Turbolaser)]
        [TestCase(WeaponFamily.FighterLaser)]
        [TestCase(WeaponFamily.FighterIon)]
        public void Caliber_ChangesBoltLengthOnly_AndPreservesColor(WeaponFamily family)
        {
            var weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(
                "Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            WeaponFamilyRule rule = weapons.GetFamily(family);
            var prefab = (BoltShot)rule.ShotPrefab;
            var sizes = new Dictionary<WeaponCaliber, Vector3>();
            foreach (WeaponCaliber caliber in new[] { WeaponCaliber.Light, WeaponCaliber.Medium, WeaponCaliber.Heavy })
            {
                WeaponProfile profile = WeaponProfileFactory.Create(new WeaponDefinition(), rule,
                    weapons.GetCaliber(caliber), weapons.GetMount(WeaponMount.Single));
                BoltTestRig.Launch(_flights, prefab, profile, _muzzle, _target, _health, _impacts);
                _flights.Advance(Time.time, 0f);
                var particles = new ParticleSystem.Particle[1];
                BoltTestRig.Particles(BoltTestRig.Layer(_flights, prefab)).GetParticles(particles, 1);
                Assert.That(particles[0].startColor, Is.EqualTo((Color32)rule.Color));
                sizes[caliber] = particles[0].startSize3D;
                _flights.Clear();
            }

            Assert.That(sizes[WeaponCaliber.Light].x, Is.GreaterThan(0f));
            Assert.That(sizes[WeaponCaliber.Light].y, Is.GreaterThan(0f));
            Assert.That(sizes[WeaponCaliber.Medium].x, Is.EqualTo(sizes[WeaponCaliber.Light].x).Within(0.001f));
            Assert.That(sizes[WeaponCaliber.Heavy].x, Is.EqualTo(sizes[WeaponCaliber.Light].x).Within(0.001f));
            Assert.That(sizes[WeaponCaliber.Medium].y, Is.GreaterThan(sizes[WeaponCaliber.Light].y));
            Assert.That(sizes[WeaponCaliber.Heavy].y, Is.GreaterThan(sizes[WeaponCaliber.Medium].y));
            // Light bolts are 0.3 of the family length; caliber never changes width.
            Vector3 light = sizes[WeaponCaliber.Light];
            Assert.That(light.x, Is.EqualTo(rule.Size.x * prefab.SizeScale).Within(0.001f));
            Assert.That(light.y, Is.EqualTo(rule.Size.y * 0.3f * prefab.SizeScale).Within(0.001f));
        }

        private GameObject Create(string name)
        {
            GameObject instance = new GameObject(name);
            _objects.Add(instance);
            return instance;
        }

        private BoltFlight Flight(BoltShot prefab) => BoltTestRig.Layer(_flights, prefab).Flights[0];
    }
}
