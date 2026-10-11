using System.Collections;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.ViewComponents.Weapon;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Weapon
{
    public sealed class ShotEffectPoolLifecycleTests
    {
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
            ShotEffectPool pool = new ShotEffectPool(profile, null, _ => { }, null, 1);
            ShotEffect effect = null;
            try
            {
                pool.Prewarm(1);
                MethodInfo acquire = typeof(ShotEffectPool).GetMethod("Acquire", BindingFlags.Instance | BindingFlags.NonPublic);
                effect = (ShotEffect)acquire.Invoke(pool, null);
                Assert.That(effect.enabled, Is.True);
                Assert.That(effect.gameObject.activeInHierarchy, Is.True);
                foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
                    particles.Play(true);

                IDictionary active = (IDictionary)typeof(ShotEffectPool)
                    .GetField("_active", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pool);
                active.Add(effect, 1);
                typeof(ShotEffectPool).GetMethod("OnEffectCompleted", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(pool, new object[] { effect, effect.LeaseId });

                Assert.That(effect.gameObject.activeInHierarchy, Is.True);
                Assert.That(effect.enabled, Is.False);
                foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
                {
                    Assert.That(particles.isStopped, Is.True, particles.name);
                    Assert.That(particles.particleCount, Is.Zero, particles.name);
                }
                foreach (LineRenderer line in effect.GetComponentsInChildren<LineRenderer>(true))
                    Assert.That(line.enabled, Is.False, line.name);
                Assert.That(acquire.Invoke(pool, null), Is.SameAs(effect));
                Assert.That(effect.enabled, Is.True);
            }
            finally
            {
                if (effect != null) Object.DestroyImmediate(effect.gameObject);
                pool.Release();
            }
        }
    }
}
