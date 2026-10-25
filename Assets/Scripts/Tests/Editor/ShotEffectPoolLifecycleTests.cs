using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.ViewComponents.Weapon;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Weapon
{
    public sealed class ShotEffectPoolLifecycleTests
    {
        // Prewarm targets cover several salvos in flight, so a pool can hold more shots than it ever fires at once.
        [TestCase(2)]
        [TestCase(4)]
        public void PrewarmedPool_KeepsAllShotsAfterLowConcurrencyUse(int count)
        {
            var weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(
                "Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            var profile = WeaponProfileFactory.Create(new WeaponDefinition(), weapons.GetFamily(WeaponFamily.Torpedo),
                weapons.GetCaliber(WeaponCaliber.Medium));
            var pool = new ShotEffectPool(profile, null, _ => { }, null, null);
            var leased = new List<ShotEffect>();
            try
            {
                while (pool.PrewarmOne(count)) { }
                int created = AttackSequenceDiagnostics.PoolCreated;
                int retired = AttackSequenceDiagnostics.PoolRetired;

                for (int i = 0; i < count; i++) Return(pool, Lease(pool));
                Assert.That(AttackSequenceDiagnostics.PoolRetired, Is.EqualTo(retired),
                    "Returning a shot must keep it for later salvos.");

                for (int i = 0; i < count; i++) leased.Add(Lease(pool));
                Assert.That(AttackSequenceDiagnostics.PoolCreated, Is.EqualTo(created),
                    "A full burst must reuse the prewarmed shots.");
                Assert.That(leased, Is.Unique);
            }
            finally
            {
                foreach (ShotEffect shot in leased)
                {
                    Return(pool, shot);
                    Object.DestroyImmediate(shot.gameObject);
                }
                pool.Release();
            }
        }

        [TestCase(WeaponFamily.Torpedo)]
        [TestCase(WeaponFamily.IonTorpedo)]
        [TestCase(WeaponFamily.FighterTorpedo)]
        public void LargeTorpedo_KeepsCompactLaunchFlashAndLongTrail(WeaponFamily family)
        {
            var weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(
                "Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            var profile = WeaponProfileFactory.Create(new WeaponDefinition(), weapons.GetFamily(family),
                weapons.GetCaliber(WeaponCaliber.Medium));
            var shot = Object.Instantiate(profile.ShotPrefab);
            var muzzle = new GameObject("Test muzzle");
            var target = new GameObject("Test target");
            var flights = new BoltFlightBatch();
            try
            {
                target.transform.position = Vector3.forward * 100f;
                shot.Fire(muzzle.transform, target.transform, target.transform, Vector3.zero, profile, flights);
                var serialized = new SerializedObject(shot);
                var flash = new SerializedObject(serialized.FindProperty("muzzleFlash").objectReferenceValue);
                var line = (LineRenderer)flash.FindProperty("flash").objectReferenceValue;
                var head = (ParticleSystem)serialized.FindProperty("head").objectReferenceValue;
                var trail = (ParticleSystem)serialized.FindProperty("trail").objectReferenceValue;

                Assert.That(head.main.startSize.constant, Is.EqualTo(9f).Within(0.001f));
                Assert.That(trail.main.startSize.constant, Is.EqualTo(5.4f).Within(0.001f));
                Assert.That(trail.main.startLifetime.constant, Is.EqualTo(0.54f).Within(0.001f));
                Assert.That(line.widthMultiplier, Is.EqualTo(3.6f).Within(0.001f));
                Assert.That(Vector3.Distance(line.GetPosition(0), line.GetPosition(1)),
                    Is.EqualTo(7.2f).Within(0.001f));
            }
            finally
            {
                flights.Dispose();
                Object.DestroyImmediate(shot.gameObject);
                Object.DestroyImmediate(muzzle);
                Object.DestroyImmediate(target);
            }
        }

        [TestCase("Projectile")]
        [TestCase("DualProjectile")]
        [TestCase("QuadProjectile")]
        [TestCase("OctupleProjectile")]
        [TestCase("LaserProjectile")]
        [TestCase("IonShot")]
        [TestCase("IonPulseShot")]
        [TestCase("HypervelocitySlugShot")]
        [TestCase("ProtonTorpedoShot")]
        [TestCase("ConcussionMissileShot")]
        public void WarmPool_ReusesActiveHierarchyAndStopsAllIdleVisuals(string name)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Prefabs/Vfx/Projectiles/{name}.prefab");
            ShotEffect source = prefab.GetComponent<ShotEffect>();
            Assert.That(source, Is.Not.Null);
            WeaponProfile profile = new WeaponProfile();
            typeof(WeaponProfile).GetField("shotPrefab", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(profile, source);
            ShotEffectPool pool = new ShotEffectPool(profile, null, _ => { }, null, null);
            ShotEffect effect = null;
            try
            {
                pool.Prewarm(1);
                effect = Lease(pool);
                Assert.That(effect.enabled, Is.True);
                Assert.That(effect.gameObject.activeInHierarchy, Is.True);
                foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
                    particles.Play(true);

                Return(pool, effect);

                Assert.That(effect.gameObject.activeInHierarchy, Is.True);
                Assert.That(effect.enabled, Is.False);
                foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
                {
                    Assert.That(particles.isStopped, Is.True, particles.name);
                    Assert.That(particles.particleCount, Is.Zero, particles.name);
                }
                foreach (LineRenderer line in effect.GetComponentsInChildren<LineRenderer>(true))
                    Assert.That(line.enabled, Is.False, line.name);
                Assert.That(Lease(pool), Is.SameAs(effect));
                Assert.That(effect.enabled, Is.True);
            }
            finally
            {
                if (effect != null)
                {
                    Return(pool, effect);
                    Object.DestroyImmediate(effect.gameObject);
                }
                pool.Release();
            }
        }

        // Play needs a live target and a ticking timer to finish a shot, so tests lease and return shots directly.
        private static ShotEffect Lease(ShotEffectPool pool)
        {
            var effect = (ShotEffect)typeof(ShotEffectPool)
                .GetMethod("Acquire", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pool, null);
            ((IDictionary)typeof(ShotEffectPool).GetField("_active", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(pool)).Add(effect, 1);
            return effect;
        }

        private static void Return(ShotEffectPool pool, ShotEffect effect)
        {
            typeof(ShotEffectPool).GetMethod("OnEffectCompleted", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(pool, new object[] { effect, effect.LeaseId });
        }
    }
}
