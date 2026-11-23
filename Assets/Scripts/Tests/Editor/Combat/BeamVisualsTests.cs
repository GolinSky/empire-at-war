using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.ViewComponents.Weapon;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Weapon
{
    [Category("Combat")]
    public sealed class BeamVisualsTests
    {
        private GameObject _root;
        private BeamVisuals _visuals;
        private LineRenderer _beam;
        private ParticleSystem _contact;
        private ParticleSystem _sparks;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Beam presentation test");
            _beam = _root.AddComponent<LineRenderer>();
            _visuals = _root.AddComponent<BeamVisuals>();
            _contact = CreateParticles("Contact");
            _sparks = CreateParticles("Sparks");
            GameObject lightObject = new GameObject("Muzzle light");
            lightObject.transform.SetParent(_root.transform);
            SetField(_visuals, "beam", _beam);
            SetField(_visuals, "chargeEffect", CreateParticles("Charge"));
            SetField(_visuals, "muzzleEffect", CreateParticles("Muzzle"));
            SetField(_visuals, "contactEffect", _contact);
            SetField(_visuals, "hullSparks", _sparks);
            SetField(_visuals, "muzzleLight", lightObject.AddComponent<Light>());
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [Test]
        public void Begin_TintsBeamGlowsAndLightWithTheBeamHue()
        {
            Color authored = new Color(0.25f, 0.65f, 1f, 1f);
            ParticleSystem.MainModule main = _contact.main;
            main.startColor = authored;
            Light light = _root.GetComponentInChildren<Light>();
            light.color = authored;

            _visuals.Begin(1f, Vector3.zero, new Color(1f, 0.45f, 0.05f, 1f));

            Color contact = _contact.main.startColor.color;
            Assert.That(contact.r, Is.GreaterThan(contact.b));
            Assert.That(light.color.r, Is.GreaterThan(light.color.b));
            Color.RGBToHSV(authored, out _, out float authoredSaturation, out float authoredValue);
            Color.RGBToHSV(contact, out _, out float saturation, out float value);
            Assert.That(saturation, Is.EqualTo(authoredSaturation).Within(0.01f));
            Assert.That(value, Is.EqualTo(authoredValue).Within(0.01f));
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            _beam.GetPropertyBlock(block);
            Color body = block.GetColor("_BodyColor");
            Assert.That(body.r, Is.GreaterThan(body.g).And.GreaterThan(body.b));

            _visuals.Begin(1f, Vector3.zero, new Color(0.04f, 0.32f, 1f, 1f));
            contact = _contact.main.startColor.color;
            Assert.That(contact.b, Is.GreaterThan(contact.r), "Tint starts from the authored colour, not the last beam.");
        }

        [Test]
        public void Contact_PersistsOnShieldsButSparksRequireArmor()
        {
            _visuals.Begin(1f, Vector3.zero, Color.cyan);
            _visuals.Tick(Vector3.zero, Vector3.forward * 100f, true, false);
            Assert.That(_contact.isEmitting, Is.True);
            Assert.That(_sparks.isEmitting, Is.False);
            _visuals.Tick(Vector3.zero, Vector3.forward * 100f, true, true);
            Assert.That(_sparks.isEmitting, Is.True);
            _visuals.Tick(Vector3.zero, Vector3.forward * 100f, true, false);
            Assert.That(_contact.isEmitting, Is.True);
            Assert.That(_sparks.isEmitting, Is.False);
        }

        [Test]
        public void Stop_FadesBeamBeforeReleasingResidualParticles()
        {
            _visuals.Begin(1f, Vector3.zero, Color.cyan);
            _visuals.Stop();
            _visuals.Tick(Vector3.zero, Vector3.forward, false, false);
            Assert.That(_beam.enabled, Is.True);
            Assert.That(_visuals.IsComplete, Is.False);
            SetField(_visuals, "_stoppedAt", Time.time - 0.2f);
            _visuals.Tick(Vector3.zero, Vector3.forward, false, false);
            Assert.That(_beam.enabled, Is.False);
            Assert.That(_visuals.IsComplete, Is.False);
            SetField(_visuals, "_stoppedAt", Time.time - 0.7f);
            _visuals.Tick(Vector3.zero, Vector3.forward, false, false);
            Assert.That(_visuals.IsComplete, Is.True);
            Assert.That(_contact.IsAlive(true), Is.False);
            Assert.That(_sparks.IsAlive(true), Is.False);
        }

        [Test]
        public void BeamShot_WaitsForResidualsAndRestartsWithoutChangingDamageDuration()
        {
            BeamShot shot = _root.AddComponent<BeamShot>();
            SetField(shot, "beam", _beam);
            SetField(shot, "hitEffect", CreateParticles("Legacy impact"));
            SetField(shot, "visuals", _visuals);
            shot.PlayBeam(_root.transform, _root.transform, 0.5f, new WeaponProfile());
            Assert.That(shot.DamageDuration, Is.EqualTo(0.5f));
            shot.StopBeam();
            MethodInfo complete = typeof(BeamShot).GetMethod("IsVisualComplete", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(complete.Invoke(shot, null), Is.False);
            SetField(_visuals, "_stoppedAt", Time.time - 0.7f);
            typeof(BeamShot).GetMethod("UpdateBeam", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(shot, null);
            Assert.That(complete.Invoke(shot, null), Is.True);
            shot.PlayBeam(_root.transform, _root.transform, 0.5f, new WeaponProfile());
            Assert.That(_beam.enabled, Is.True);
            Assert.That(_visuals.IsComplete, Is.False);
        }

        private ParticleSystem CreateParticles(string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(_root.transform);
            ParticleSystem particles = child.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return particles;
        }

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
