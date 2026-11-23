using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Models.Factions;
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
        private const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void SharedPool_ReturnsReleasedOwnersInflightShotToAnotherOwner()
        {
            WeaponProfile profile = Profile();
            var prewarmer = new ShotPoolPrewarmer(null);
            ShotEffectPool first = Pool(prewarmer, profile);
            ShotEffectPool second = Pool(prewarmer, profile);
            int retired = 0;
            first.Retired += _ => retired++;
            ShotEffect effect = null;
            try
            {
                Prewarm(first, 1);
                effect = Lease(first);
                int created = AttackSequenceDiagnostics.PoolCreated;
                first.Release();
                Assert.That(retired, Is.Zero, "A released mount retires only after its last shot lands.");
                Return(first, effect);
                Assert.That(retired, Is.EqualTo(1));

                ShotEffect reused = Lease(second);
                Assert.That(reused, Is.SameAs(effect), "Reinforcement mounts must share the same idle shots.");
                Assert.That(AttackSequenceDiagnostics.PoolCreated, Is.EqualTo(created));
                Return(second, reused);
            }
            finally
            {
                if (effect != null) Object.DestroyImmediate(effect.gameObject);
                prewarmer.Clear();
            }
        }

        [Test]
        public void EmptyPool_SkipsTheVisualWithoutCreatingAndGrowsLater()
        {
            WeaponProfile profile = Profile();
            var prewarmer = new ShotPoolPrewarmer(null);
            ShotEffectPool pool = Pool(prewarmer, profile);
            ShotEffectCache cache = Cache(pool);
            int demandChanges = 0;
            cache.DemandChanged += _ => demandChanges++;
            try
            {
                int created = AttackSequenceDiagnostics.PoolCreated;
                // A skipped shot never touches its target, so the attack and target may be absent here.
                pool.Play(null, null, Vector3.zero, true, null, 0f);

                Assert.That(pool.ActiveCount, Is.Zero);
                Assert.That(AttackSequenceDiagnostics.PoolCreated, Is.EqualTo(created), "Firing never instantiates.");
                Assert.That(cache.SkippedVisuals, Is.EqualTo(1));
                Assert.That(cache.PendingDemand, Is.EqualTo(1));
                Assert.That(cache.RequiredCount, Is.EqualTo(1));
                Assert.That(demandChanges, Is.EqualTo(1));
                Assert.That(cache.TakeWindowSkippedVisuals(), Is.EqualTo(1));
                Assert.That(cache.TakeWindowSkippedVisuals(), Is.Zero);

                prewarmer.Tick();
                Assert.That(cache.TotalCount, Is.GreaterThanOrEqualTo(1), "Runtime growth serves the missed shot.");
                Assert.That(cache.PendingDemand, Is.Zero);
                Assert.That(Lease(pool), Is.Not.Null);
            }
            finally { prewarmer.Clear(); }
        }

        [Test]
        public void PendingDemand_IsCappedWhileEveryMissStillCounts()
        {
            var cache = new ShotEffectCache(Profile().ShotPrefab);
            for (int i = 0; i < 1000; i++) Assert.That(cache.TryAcquire(out _), Is.False);
            Assert.That(cache.SkippedVisuals, Is.EqualTo(1000));
            Assert.That(cache.PendingDemand, Is.GreaterThan(0));
            Assert.That(cache.PendingDemand, Is.LessThan(1000), "One burst must not queue unbounded growth.");
            cache.DestroyAll();
        }

        [Test]
        public void RuntimeGrowth_GetsALargerBudgetOnlyWhileShotsFireUndrawn()
        {
            WeaponProfile profile = Profile();
            var prewarmer = new ShotPoolPrewarmer(null);
            ShotEffectPool pool = Pool(prewarmer, profile);
            MethodInfo budget = typeof(ShotPoolPrewarmer).GetMethod("GetBudgetMilliseconds", PRIVATE_INSTANCE);
            try
            {
                double idle = (double)budget.Invoke(prewarmer, null);
                pool.Play(null, null, Vector3.zero, false, null, 0f);
                Assert.That((double)budget.Invoke(prewarmer, null), Is.GreaterThan(idle));
            }
            finally { prewarmer.Clear(); }
        }

        [Test]
        public void BuildStock_GrowsWithSlotCountPerFaction()
        {
            ShotEffect prefab = Profile().ShotPrefab;
            System.Func<FactionType, Dictionary<ShotEffect, int>> demand =
                _ => new Dictionary<ShotEffect, int> { { prefab, 5 } };
            var one = new Dictionary<FactionType, int> { { FactionType.Republic, 1 } };
            var many = new Dictionary<FactionType, int> { { FactionType.Republic, 3 }, { FactionType.Empire, 2 } };
            Assert.That(ShotPoolPrewarmer.BuildStock(many, demand)[prefab],
                Is.GreaterThan(ShotPoolPrewarmer.BuildStock(one, demand)[prefab]));
        }

        [Test]
        public void StrikecraftAndShips_ShareNoCache()
        {
            WeaponProfile profile = Profile();
            var prewarmer = new ShotPoolPrewarmer(null);
            ShotEffectPool ship = Pool(prewarmer, profile);
            var fighter = (ShotEffectPool)prewarmer.CreateVisuals(profile, null, null, 1000, true);
            try
            {
                Assert.That(Cache(fighter), Is.Not.SameAs(Cache(ship)));
                Assert.That(Cache(fighter).IsStrikecraftGroup, Is.True);
                fighter.Play(null, null, Vector3.zero, false, null, 0f);
                Assert.That(Cache(fighter).PendingDemand, Is.EqualTo(1));
                Assert.That(Cache(ship).PendingDemand, Is.Zero, "Fighter demand must not grow the ship cache.");
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
        public void WarmLoadout_SumsNormalCadenceStockAndNewMountsAllocateNothing()
        {
            WeaponProfile profile = Profile(WeaponFamily.Torpedo);
            var prewarmer = new ShotPoolPrewarmer(null);
            try
            {
                prewarmer.RequestPrewarm(new[] { profile, profile });
                var targets = (IDictionary)typeof(ShotPoolPrewarmer).GetField("_targets", PRIVATE_INSTANCE)
                    .GetValue(prewarmer);
                Assert.That(targets.Count, Is.EqualTo(1));
                int baseline = 2 * WeaponHardPoint.ExpectedShotsInFlight(profile);
                foreach (DictionaryEntry target in targets) Assert.That(target.Value, Is.EqualTo(baseline));
                ShotEffectPool pool = Pool(prewarmer, profile, WeaponHardPoint.ExpectedShotsInFlight(profile, 0.1f, 0.1f));
                Assert.That(Cache(pool).TotalCount, Is.Zero, "Creating a mount's visuals never instantiates.");
                Assert.That(Cache(pool).RequiredCount, Is.Zero);
                foreach (DictionaryEntry target in targets) Assert.That(target.Value, Is.EqualTo(baseline));
            }
            finally { prewarmer.Clear(); }
        }

        [Test]
        public void BoltProfiles_DrawThroughTheSharedBoltLayerAndBuildNoCache()
        {
            WeaponProfile profile = Profile(WeaponFamily.Turbolaser);
            Assert.That(profile.ShotPrefab, Is.InstanceOf<BoltShot>());
            var flights = new BoltFlightBatch();
            var prewarmer = new ShotPoolPrewarmer(flights);
            try
            {
                prewarmer.RequestPrewarm(new[] { profile });
                Assert.That(prewarmer.Caches, Is.Empty, "Bolts are particles of a shared layer, never pooled objects.");
                Assert.That(BoltTestRig.Layer(flights, (BoltShot)profile.ShotPrefab), Is.Not.Null);
                Assert.That(prewarmer.CreateVisuals(profile, null, null, 10), Is.InstanceOf<BoltShotVisuals>());
                Assert.That(prewarmer.Caches, Is.Empty);
            }
            finally
            {
                prewarmer.Clear();
                flights.Dispose();
            }
        }

        [TestCase(1f)]
        [TestCase(0.1f)]
        public void SteadySalvos_ReuseExpectedStockWithoutSkippingOrCreating(float cadence)
        {
            WeaponProfile profile = Profile();
            int cap = WeaponHardPoint.ExpectedShotsInFlight(profile, cadence, cadence);
            var prewarmer = new ShotPoolPrewarmer(null);
            ShotEffectPool pool = Pool(prewarmer, profile, cap);
            var flying = new Queue<ShotEffect>();
            try
            {
                Prewarm(pool, cap);
                int created = AttackSequenceDiagnostics.PoolCreated;
                for (int salvo = 0; salvo < 100; salvo++)
                {
                    while (flying.Count + profile.ShotsPerSalvo > cap) Return(pool, flying.Dequeue());
                    for (int shot = 0; shot < profile.ShotsPerSalvo; shot++) flying.Enqueue(Lease(pool));
                }
                while (flying.Count > 0) Return(pool, flying.Dequeue());
                Assert.That(Cache(pool).SkippedVisuals, Is.Zero);
                Assert.That(AttackSequenceDiagnostics.PoolCreated, Is.EqualTo(created));
                Assert.That(Cache(pool).PeakInUse, Is.EqualTo(cap));
            }
            finally { prewarmer.Clear(); }
        }

        [Test]
        public void ScheduledSalvo_StartsWithNoShotVisualAndStartsTheReload()
        {
            WeaponProfile profile = Profile();
            var prewarmer = new ShotPoolPrewarmer(null);
            var mount = new GameObject("Visual-free mount");
            try
            {
                var weapon = mount.AddComponent<WeaponHardPoint>();
                var modifiers = new CombatModifiers();
                weapon.SetData(profile, profile.Range, 0f, null, null, modifiers, null, null, prewarmer);
                weapon.PrepareEffects();
                var pool = (ShotEffectPool)typeof(WeaponHardPoint).GetField("_visuals", PRIVATE_INSTANCE)
                    .GetValue(weapon);
                object reload = typeof(WeaponHardPoint).GetField("_reloadTimer", PRIVATE_INSTANCE).GetValue(weapon);
                Assert.That(Cache(pool).TotalCount, Is.Zero);

                Assert.That(weapon.TryStartScheduledSequence(out int generation), Is.True,
                    "An empty visual pool must never hold a mount.");
                Assert.That(reload.GetType().GetProperty("HasStarted").GetValue(reload), Is.True);
                Assert.That(weapon.IsEmitting(generation), Is.True);
                weapon.StopEmitting(generation);

                modifiers.SetIonDisabled(true);
                Assert.That(weapon.TryStartScheduledSequence(out _), Is.False, "Power state still gates firing.");
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
            WeaponProfile profile = Profile();
            var cache = new ShotEffectCache(profile.ShotPrefab);
            var pool = new ShotEffectPool(profile, null, null, cache);
            var leased = new List<ShotEffect>();
            try
            {
                while (cache.PrewarmOne(count)) { }
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
                foreach (ShotEffect shot in leased) Return(pool, shot);
                pool.Release();
                cache.DestroyAll();
            }
        }

        [TestCase(WeaponFamily.Torpedo)]
        [TestCase(WeaponFamily.IonTorpedo)]
        [TestCase(WeaponFamily.FighterTorpedo)]
        public void LargeTorpedo_KeepsCompactLaunchFlashAndUncappedTrail(WeaponFamily family)
        {
            var weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(
                "Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            var profile = WeaponProfileFactory.Create(new WeaponDefinition(), weapons.GetFamily(family),
                weapons.GetCaliber(WeaponCaliber.Medium), weapons.GetMount(WeaponMount.Single));
            var shot = Object.Instantiate(profile.ShotPrefab);
            var muzzle = new GameObject("Test muzzle");
            var target = new GameObject("Test target");
            try
            {
                target.transform.position = Vector3.forward * 100f;
                shot.Fire(muzzle.transform, target.transform, target.transform, Vector3.zero, profile);
                var serialized = new SerializedObject(shot);
                var flash = new SerializedObject(serialized.FindProperty("muzzleFlash").objectReferenceValue);
                var line = (LineRenderer)flash.FindProperty("flash").objectReferenceValue;
                var head = (ParticleSystem)serialized.FindProperty("head").objectReferenceValue;
                var trail = (ParticleSystem)serialized.FindProperty("trail").objectReferenceValue;

                Assert.That(head.main.startSize.constant, Is.EqualTo(9f).Within(0.001f));
                Assert.That(trail.main.startSize.constant, Is.EqualTo(5.4f).Within(0.001f));
                // The particle cap must hold a whole trail at flight speed, or the trail is cut short mid-flight.
                float liveParticles = trail.emission.rateOverDistance.constant * trail.main.startLifetime.constant *
                                      profile.ProjectileSpeed;
                Assert.That(trail.main.startLifetime.constant, Is.GreaterThan(0f));
                Assert.That(trail.main.maxParticles, Is.GreaterThanOrEqualTo(Mathf.CeilToInt(liveParticles)));
                Assert.That(line.widthMultiplier, Is.EqualTo(3.6f).Within(0.001f));
                Assert.That(Vector3.Distance(line.GetPosition(0), line.GetPosition(1)),
                    Is.EqualTo(7.2f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(shot.gameObject);
                Object.DestroyImmediate(muzzle);
                Object.DestroyImmediate(target);
            }
        }

        [TestCase("LaserProjectile")]
        [TestCase("IonShot")]
        [TestCase("IonPulseShot")]
        [TestCase("ProtonTorpedoShot")]
        [TestCase("ConcussionMissileShot")]
        public void WarmPool_ReusesActiveHierarchyAndStopsAllIdleVisuals(string name)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Prefabs/Vfx/Projectiles/{name}.prefab");
            ShotEffect source = prefab.GetComponent<ShotEffect>();
            Assert.That(source, Is.Not.Null);
            WeaponProfile profile = new WeaponProfile();
            typeof(WeaponProfile).GetField("shotPrefab", PRIVATE_INSTANCE).SetValue(profile, source);
            var cache = new ShotEffectCache(source);
            var pool = new ShotEffectPool(profile, null, null, cache);
            ShotEffect effect = null;
            try
            {
                cache.PrewarmOne(1);
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
                if (effect != null) Return(pool, effect);
                pool.Release();
                cache.DestroyAll();
            }
        }

        [TestCase("Projectile")]
        [TestCase("QuadProjectile")]
        public void BoltPrefab_RefusesToFireAsAPooledObject(string name)
        {
            BoltShot bolt = Object.Instantiate(BoltTestRig.LoadBolt(name));
            var muzzle = new GameObject("Test muzzle");
            try
            {
                Assert.Throws<System.InvalidOperationException>(() => bolt.Fire(muzzle.transform, muzzle.transform,
                    muzzle.transform, Vector3.zero, BoltTestRig.SpeedProfile(100f)));
            }
            finally
            {
                Object.DestroyImmediate(bolt.gameObject);
                Object.DestroyImmediate(muzzle);
            }
        }

        [Test]
        public void FlightSeconds_FollowEachShotKind()
        {
            Vector3 muzzle = Vector3.zero;
            // Height is ignored: shots between ships on different heights take as long as level shots.
            Vector3 aim = new Vector3(0f, 30f, 40f);
            WeaponProfile profile = Profile(WeaponFamily.Torpedo);
            ShotEffect missile = profile.ShotPrefab;
            float travel = 40f / profile.ProjectileSpeed;
            Assert.That(missile.GetFlightSeconds(muzzle, aim, profile, 0f), Is.EqualTo(travel).Within(0.0001f));
            Assert.That(missile.GetFlightSeconds(muzzle, aim, profile, travel * 0.25f),
                Is.EqualTo(travel * 0.75f).Within(0.0001f), "A late shot starts that far into its flight.");
            Assert.That(missile.GetFlightSeconds(muzzle, aim, profile, travel * 2f), Is.Zero);

            var beam = AssetDatabase.LoadAssetAtPath<BeamShot>("Assets/Prefabs/Vfx/Projectiles/LaserProjectile.prefab");
            Assert.That(beam.GetFlightSeconds(muzzle, aim, profile, 1f),
                Is.EqualTo(new SerializedObject(beam).FindProperty("growthDuration").floatValue));
            var pulse = AssetDatabase.LoadAssetAtPath<IonPulseShot>("Assets/Prefabs/Vfx/Projectiles/IonPulseShot.prefab");
            Assert.That(pulse.GetFlightSeconds(muzzle, aim, profile, 1f),
                Is.EqualTo(new SerializedObject(pulse).FindProperty("flightDuration").floatValue));
        }

        private static WeaponProfile Profile(WeaponFamily family = WeaponFamily.Torpedo)
        {
            var weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(
                "Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            return WeaponProfileFactory.Create(new WeaponDefinition(), weapons.GetFamily(family),
                weapons.GetCaliber(WeaponCaliber.Medium), weapons.GetMount(WeaponMount.Single));
        }

        private static ShotEffectPool Pool(ShotPoolPrewarmer prewarmer, WeaponProfile profile, int capacity = 1000) =>
            (ShotEffectPool)prewarmer.CreateVisuals(profile, null, null, capacity);

        private static ShotEffectCache Cache(ShotEffectPool pool) => (ShotEffectCache)typeof(ShotEffectPool)
            .GetField("_cache", PRIVATE_INSTANCE).GetValue(pool);

        private static void Prewarm(ShotEffectPool pool, int count)
        {
            ShotEffectCache cache = Cache(pool);
            while (cache.PrewarmOne(count)) { }
        }

        // Play needs a live target and a ticking timer to finish a shot, so tests lease and return shots directly.
        private static ShotEffect Lease(ShotEffectPool pool)
        {
            Assert.That(Cache(pool).TryAcquire(out ShotEffect effect), Is.True);
            ((HashSet<ShotEffect>)typeof(ShotEffectPool).GetField("_active", PRIVATE_INSTANCE).GetValue(pool))
                .Add(effect);
            return effect;
        }

        private static void Return(ShotEffectPool pool, ShotEffect effect)
        {
            typeof(ShotEffectPool).GetMethod("OnEffectCompleted", PRIVATE_INSTANCE)
                .Invoke(pool, new object[] { effect, effect.LeaseId });
        }
    }
}
