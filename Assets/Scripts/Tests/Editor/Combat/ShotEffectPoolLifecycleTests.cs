using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.ViewComponents.Weapon;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Weapon
{
    [Category("Combat")]
    public sealed class ShotEffectPoolLifecycleTests
    {
        [Test]
        public void SharedPool_ReturnsReleasedOwnersInflightShotToAnotherOwner()
        {
            var weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(
                "Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            var profile = WeaponProfileFactory.Create(new WeaponDefinition(), weapons.GetFamily(WeaponFamily.Torpedo),
                weapons.GetCaliber(WeaponCaliber.Medium), weapons.GetMount(WeaponMount.Single));
            var prewarmer = new ShotPoolPrewarmer();
            int firstCompletions = 0;
            int secondCompletions = 0;
            ShotEffectPool first = prewarmer.CreatePool(profile, null, _ => firstCompletions++, null, null, 1);
            ShotEffectPool second = prewarmer.CreatePool(profile, null, _ => secondCompletions++, null, null, 1);
            ShotEffect effect = null;
            try
            {
                first.Prewarm(1);
                effect = Lease(first);
                int created = AttackSequenceDiagnostics.PoolCreated;
                first.Release();
                Return(first, effect);
                Assert.That(firstCompletions, Is.Zero, "A released mount must not receive flight completion.");

                ShotEffect reused = Lease(second);
                Assert.That(reused, Is.SameAs(effect), "Reinforcement mounts must share the same idle shots.");
                Assert.That(AttackSequenceDiagnostics.PoolCreated, Is.EqualTo(created));
                Return(second, reused);
                Assert.That(secondCompletions, Is.EqualTo(1));
            }
            finally
            {
                if (effect != null) Object.DestroyImmediate(effect.gameObject);
                prewarmer.Clear();
            }
        }

        [Test]
        public void SharedAdmission_ReservesEntireSalvoAndReusesOwnerlessShots()
        {
            WeaponProfile profile = Profile();
            var prewarmer = new ShotPoolPrewarmer();
            ShotEffectPool first = prewarmer.CreatePool(profile, null, _ => { }, null, null, 1000);
            ShotEffectPool second = prewarmer.CreatePool(profile, null, _ => { }, null, null, 1000);
            try
            {
                first.Prewarm(3);
                Assert.That(first.TryReserveSalvo(2), Is.True);
                Assert.That(second.TryReserveSalvo(2), Is.False, "One spare shot cannot admit half a salvo.");
                ShotEffect a = Lease(first);
                ShotEffect b = Lease(first);
                int created = AttackSequenceDiagnostics.PoolCreated;
                first.Release();
                Assert.That(second.TryReserveSalvo(2), Is.False);
                Return(first, a);
                Assert.That(second.TryReserveSalvo(2), Is.True, "Completed orphan leases immediately serve another mount.");
                ShotEffect c = Lease(second);
                ShotEffect d = Lease(second);
                Assert.That(c, Is.Not.SameAs(d));
                Assert.That(c, Is.Not.SameAs(b));
                Assert.That(d, Is.Not.SameAs(b));
                Return(first, b);
                Return(second, c);
                Return(second, d);
                Assert.That(AttackSequenceDiagnostics.PoolCreated, Is.EqualTo(created));
                Assert.That(Cache(first).PeakInUse, Is.EqualTo(3));
            }
            finally { prewarmer.Clear(); }
        }

        [Test]
        public void SharedAdmission_SmallSalvoIsAdmittedPastLargerWaiterAtTheHead()
        {
            WeaponProfile profile = Profile();
            var prewarmer = new ShotPoolPrewarmer();
            ShotEffectPool first = prewarmer.CreatePool(profile, null, _ => { }, null, null, 1000);
            ShotEffectPool second = prewarmer.CreatePool(profile, null, _ => { }, null, null, 1000);
            try
            {
                first.Prewarm(1);
                Assert.That(first.TryReserveSalvo(2), Is.False);
                Assert.That(second.TryReserveSalvo(1), Is.True, "A salvo that fits must not wait behind a larger one.");
                ShotEffect a = Lease(second);
                first.Prewarm(3);
                Assert.That(first.TryReserveSalvo(2), Is.True);
                ShotEffect b = Lease(first);
                ShotEffect c = Lease(first);
                Return(second, a);
                Return(first, b);
                Return(first, c);
            }
            finally { prewarmer.Clear(); }
        }

        [Test]
        public void StrikecraftAndShips_ShareNoCacheOrAdmissionQueue()
        {
            WeaponProfile profile = Profile();
            var prewarmer = new ShotPoolPrewarmer();
            ShotEffectPool ship = prewarmer.CreatePool(profile, null, _ => { }, null, null, 1000);
            ShotEffectPool fighter = prewarmer.CreatePool(profile, null, _ => { }, null, null, 1000, true);
            try
            {
                Assert.That(Cache(fighter), Is.Not.SameAs(Cache(ship)));
                Assert.That(Cache(fighter).IsStrikecraftGroup, Is.True);
                Assert.That(fighter.TryReserveSalvo(3), Is.False);
                Assert.That(Cache(ship).WaitingCount, Is.Zero, "Fighter demand must not queue on the ship cache.");
            }
            finally { prewarmer.Clear(); }
        }

        [TestCase(WeaponFamily.Turbolaser)]
        [TestCase(WeaponFamily.Torpedo)]
        public void ExpectedShotsInFlight_CoversFlightPlusPostImpactLease(WeaponFamily family)
        {
            WeaponProfile profile = Profile(family);
            float lifetime = (profile.ProjectileSpeed > 0f ? profile.Range / profile.ProjectileSpeed : 0f) +
                             profile.ShotPrefab.PostImpactSeconds;
            float cycle = Mathf.Max(profile.Reload, (profile.ShotsPerSalvo - 1) * profile.ShotInterval, 0.01f);
            Assert.That(WeaponHardPoint.ExpectedShotsInFlight(profile),
                Is.GreaterThanOrEqualTo(profile.ShotsPerSalvo * Mathf.CeilToInt(lifetime / cycle)));
        }

        [Test]
        public void CancelledAdmission_RemovesGrowthDemandAndUnblocksNextMount()
        {
            WeaponProfile profile = Profile();
            var prewarmer = new ShotPoolPrewarmer();
            ShotEffectPool first = prewarmer.CreatePool(profile, null, _ => { }, null, null, 1000);
            ShotEffectPool second = prewarmer.CreatePool(profile, null, _ => { }, null, null, 1000);
            try
            {
                first.Prewarm(1);
                Assert.That(first.TryReserveSalvo(100), Is.False);
                Assert.That(Cache(first).WaitingCount, Is.EqualTo(100));
                first.Release();
                Assert.That(Cache(first).WaitingCount, Is.Zero);
                Assert.That(second.TryReserveSalvo(1), Is.True);
                second.CancelReservation();
                int created = AttackSequenceDiagnostics.PoolCreated;
                prewarmer.Tick();
                Assert.That(AttackSequenceDiagnostics.PoolCreated, Is.EqualTo(created), "Retired demand must not create obsolete shots.");
                Assert.That(Cache(first).RequiredCount, Is.Zero);
            }
            finally { prewarmer.Clear(); }
        }

        [TestCase(WeaponFamily.Turbolaser)]
        [TestCase(WeaponFamily.Torpedo)]
        public void WarmLoadout_UsesNormalCadenceWhileBoostedLogicalCapacityDoesNotAllocate(WeaponFamily family)
        {
            WeaponProfile profile = Profile(family);
            var prewarmer = new ShotPoolPrewarmer();
            try
            {
                prewarmer.RequestPrewarm(new[] { profile, profile });
                var targets = (IDictionary)typeof(ShotPoolPrewarmer)
                    .GetField("_targets", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(prewarmer);
                Assert.That(targets.Count, Is.EqualTo(1));
                int baseline = 2 * WeaponHardPoint.ExpectedShotsInFlight(profile);
                foreach (DictionaryEntry target in targets) Assert.That(target.Value, Is.EqualTo(baseline));
                int boosted = WeaponHardPoint.ExpectedShotsInFlight(profile, 0.1f, 0.1f);
                Assert.That(boosted, Is.GreaterThan(WeaponHardPoint.ExpectedShotsInFlight(profile)));
                ShotEffectPool pool = prewarmer.CreatePool(profile, null, _ => { }, null, null, boosted * 100);
                Assert.That(Cache(pool).TotalCount, Is.Zero);
                Assert.That(Cache(pool).RequiredCount, Is.Zero);
                foreach (DictionaryEntry target in targets) Assert.That(target.Value, Is.EqualTo(baseline));
            }
            finally { prewarmer.Clear(); }
        }

        [Test]
        public void Admission_RejectsOverflowWithoutSynchronousCreation()
        {
            WeaponProfile profile = Profile();
            var prewarmer = new ShotPoolPrewarmer();
            ShotEffectPool pool = prewarmer.CreatePool(profile, null, _ => { }, null, null, 1000);
            try
            {
                int created = AttackSequenceDiagnostics.PoolCreated;
                Assert.That(pool.TryReserveSalvo(2), Is.False);
                Assert.That(Cache(pool).RequiredCount, Is.EqualTo(2));
                Assert.That(AttackSequenceDiagnostics.PoolCreated, Is.EqualTo(created));
                Assert.Throws<System.InvalidOperationException>(() => Cache(pool).Acquire(pool));
                pool.Prewarm(2);
                Assert.That(pool.TryReserveSalvo(2, 2), Is.True);
                ShotEffect a = Lease(pool);
                ShotEffect b = Lease(pool);
                Assert.That(pool.TryReserveSalvo(1, 2), Is.False, "Physical availability cannot bypass the mount's overlap cap.");
                Assert.That(Cache(pool).WaitingCount, Is.Zero);
                Return(pool, a);
                Assert.That(pool.TryReserveSalvo(1, 2), Is.True);
                pool.CancelReservation();
                Return(pool, b);
                Assert.That(Cache(pool).OnDemandExpansions, Is.Zero);
            }
            finally { prewarmer.Clear(); }
        }

        [TestCase(1f)]
        [TestCase(0.1f)]
        public void SteadySalvos_ReuseFullNormalOrBoostedOverlapWithoutAdmissionWait(float cadence)
        {
            WeaponProfile profile = Profile();
            int cap = WeaponHardPoint.ExpectedShotsInFlight(profile, cadence, cadence);
            var prewarmer = new ShotPoolPrewarmer();
            ShotEffectPool pool = prewarmer.CreatePool(profile, null, _ => { }, null, null, cap);
            var flying = new Queue<ShotEffect>();
            try
            {
                pool.Prewarm(cap);
                int created = AttackSequenceDiagnostics.PoolCreated;
                for (int salvo = 0; salvo < 100; salvo++)
                {
                    while (flying.Count + profile.ShotsPerSalvo > cap) Return(pool, flying.Dequeue());
                    Assert.That(pool.TryReserveSalvo(profile.ShotsPerSalvo, cap), Is.True);
                    for (int shot = 0; shot < profile.ShotsPerSalvo; shot++) flying.Enqueue(Lease(pool));
                }
                while (flying.Count > 0) Return(pool, flying.Dequeue());
                Assert.That(Cache(pool).AdmissionWaits, Is.Zero);
                Assert.That(AttackSequenceDiagnostics.PoolCreated, Is.EqualTo(created));
                Assert.That(Cache(pool).PeakInUse, Is.EqualTo(cap));
            }
            finally { prewarmer.Clear(); }
        }

        [Test]
        public void ScheduledSalvo_WaitsBeforeReloadAndReturnsUnusedReservationOnCancellation()
        {
            WeaponProfile profile = Profile();
            var prewarmer = new ShotPoolPrewarmer();
            var mount = new GameObject("Admission mount");
            try
            {
                var weapon = mount.AddComponent<WeaponHardPoint>();
                var modifiers = new CombatModifiers();
                weapon.SetData(profile, profile.Range, 0f, null, null, modifiers, null, null, null, prewarmer);
                weapon.PrepareEffects();
                var pool = (ShotEffectPool)typeof(WeaponHardPoint).GetField("_effectPool",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(weapon);
                object reload = typeof(WeaponHardPoint).GetField("_reloadTimer",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(weapon);
                var sequence = (AttackSequenceState)typeof(WeaponHardPoint).GetField("_sequence",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(weapon);
                Assert.That(weapon.TryStartScheduledSequence(out _), Is.False);
                Assert.That(reload.GetType().GetProperty("HasStarted").GetValue(reload), Is.False,
                    "Waiting for storage must not start the reload clock.");
                Assert.That(sequence.CurrentStatus, Is.EqualTo(AttackSequenceState.Status.Ready),
                    "Waiting for storage must not start the sequence.");
                Assert.That(Cache(pool).WaitingCount, Is.EqualTo(profile.ShotsPerSalvo));
                modifiers.SetIonDisabled(true);
                weapon.RefreshTiming();
                Assert.That(Cache(pool).WaitingCount, Is.Zero);
                modifiers.SetIonDisabled(false);
                pool.Prewarm(profile.ShotsPerSalvo);
                Assert.That(weapon.TryStartScheduledSequence(out int generation), Is.True);
                Assert.That(Cache(pool).ReservedCount, Is.EqualTo(profile.ShotsPerSalvo));
                weapon.StopEmitting(generation - 1);
                Assert.That(Cache(pool).ReservedCount, Is.EqualTo(profile.ShotsPerSalvo));
                weapon.StopEmitting(generation);
                Assert.That(Cache(pool).ReservedCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(mount);
                prewarmer.Clear();
            }
        }

        // Prewarm targets cover several salvos in flight, so a pool can hold more shots than it ever fires at once.
        [TestCase(2)]
        [TestCase(4)]
        public void PrewarmedPool_KeepsAllShotsAfterLowConcurrencyUse(int count)
        {
            var weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(
                "Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            var profile = WeaponProfileFactory.Create(new WeaponDefinition(), weapons.GetFamily(WeaponFamily.Torpedo),
                weapons.GetCaliber(WeaponCaliber.Medium), weapons.GetMount(WeaponMount.Single));
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
                weapons.GetCaliber(WeaponCaliber.Medium), weapons.GetMount(WeaponMount.Single));
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

        private static WeaponProfile Profile(WeaponFamily family = WeaponFamily.Torpedo)
        {
            var weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(
                "Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            return WeaponProfileFactory.Create(new WeaponDefinition(), weapons.GetFamily(family),
                weapons.GetCaliber(WeaponCaliber.Medium), weapons.GetMount(WeaponMount.Single));
        }

        private static ShotEffectCache Cache(ShotEffectPool pool) => (ShotEffectCache)typeof(ShotEffectPool)
            .GetField("_cache", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pool);

        // Play needs a live target and a ticking timer to finish a shot, so tests lease and return shots directly.
        private static ShotEffect Lease(ShotEffectPool pool)
        {
            if (pool.ReservedShots == 0) Assert.That(pool.TryReserveSalvo(1), Is.True);
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
