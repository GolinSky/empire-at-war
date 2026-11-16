using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using EmpireAtWar.Repository;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>
    /// Owns per-prefab battle storage; loading warms roster demand and runtime growth is capped per frame.
    /// </summary>
    public sealed class ShotPoolPrewarmer : ITickable, ILateDisposable
    {
        private const double BUDGET_MILLISECONDS = 0.5;
        private const double LOADING_BUDGET_MILLISECONDS = 4.0;
        private const int MAX_CREATIONS_PER_FRAME = 1;

        private readonly Queue<ShotEffectCache> _pending = new Queue<ShotEffectCache>();
        private readonly HashSet<ShotEffectCache> _queued = new HashSet<ShotEffectCache>();
        private readonly Dictionary<ShotEffect, ShotEffectCache> _caches =
            new Dictionary<ShotEffect, ShotEffectCache>();
        private readonly Dictionary<ShotEffectCache, int> _targets = new Dictionary<ShotEffectCache, int>();
        private readonly Dictionary<ShotEffectPool, (ShotEffectCache Cache, int Capacity)> _pools =
            new Dictionary<ShotEffectPool, (ShotEffectCache, int)>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public IEnumerable<ShotEffectCache> Caches => _caches.Values;
#endif

        public ShotEffectPool CreatePool(WeaponProfile profile, Transform owner, Action<int> completed,
            ImpactEffectPresenter impacts, BoltFlightBatch flights, int target)
        {
            ShotEffectCache cache = GetCache(profile.ShotPrefab);
            cache.RegisterCapacity(target);
            var pool = new ShotEffectPool(profile, owner, completed, impacts, flights, cache, target);
            _pools.Add(pool, (cache, target));
            pool.Retired += OnRetired;
            return pool;
        }

        internal void IncreaseCapacity(ShotEffectPool pool, int capacity)
        {
            (ShotEffectCache cache, int previousCapacity) = _pools[pool];
            cache.RegisterCapacity(capacity - previousCapacity);
            _pools[pool] = (cache, capacity);
            pool.IncreaseCapacity(capacity);
        }

        private ShotEffectCache GetCache(ShotEffect prefab)
        {
            if (!_caches.TryGetValue(prefab, out ShotEffectCache cache))
            {
                cache = new ShotEffectCache(prefab);
                cache.DemandChanged += Enqueue;
                _caches.Add(prefab, cache);
                _targets.Add(cache, 0);
            }
            return cache;
        }

        private void Enqueue(ShotEffectCache cache)
        {
            if (_queued.Add(cache)) _pending.Enqueue(cache);
        }

        public void RequestPrewarm(IReadOnlyList<WeaponProfile> profiles)
        {
            var targets = new Dictionary<ShotEffect, int>();
            foreach (WeaponProfile profile in profiles)
            {
                targets.TryGetValue(profile.ShotPrefab, out int count);
                targets[profile.ShotPrefab] = count + WeaponHardPoint.ExpectedShotsInFlight(profile);
            }
            foreach (KeyValuePair<ShotEffect, int> target in targets) WarmLoadout(GetCache(target.Key), target.Value);
        }

        private void WarmLoadout(ShotEffectCache cache, int target)
        {
            _targets[cache] = Math.Max(_targets[cache], target);
            Enqueue(cache);
        }

        private bool PrewarmNext(bool loading)
        {
            ShotEffectCache cache = _pending.Dequeue();
            _queued.Remove(cache);
            int target = Math.Max(_targets[cache], cache.RequiredCount);
            bool created = cache.PrewarmOne(target, loading);
            if (cache.TotalCount < target) Enqueue(cache);
            return created;
        }

        public async Awaitable CompletePrewarmAsync(CancellationToken cancellationToken)
        {
            long start = Stopwatch.GetTimestamp();
            long budget = (long)(LOADING_BUDGET_MILLISECONDS * Stopwatch.Frequency / 1000.0);
            while (_pending.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!PrewarmNext(true)) continue;
                if (Stopwatch.GetTimestamp() - start < budget) continue;
                await Awaitable.NextFrameAsync(cancellationToken);
                start = Stopwatch.GetTimestamp();
            }
        }

        private void OnRetired(ShotEffectPool pool)
        {
            pool.Retired -= OnRetired;
            (ShotEffectCache cache, int capacity) = _pools[pool];
            cache.ReleaseCapacity(capacity);
            _pools.Remove(pool);
        }

        public void Clear()
        {
            _pending.Clear();
            _queued.Clear();
            _targets.Clear();
            foreach (ShotEffectPool pool in new List<ShotEffectPool>(_pools.Keys)) pool.DestroyAll();
            foreach (ShotEffectCache cache in _caches.Values)
            {
                cache.DemandChanged -= Enqueue;
                cache.DestroyAll();
            }
            _caches.Clear();
        }

        public void LateDispose() => Clear();

        public void Tick()
        {
            long start = Stopwatch.GetTimestamp();
            long budget = (long)(BUDGET_MILLISECONDS * Stopwatch.Frequency / 1000.0);
            int created = 0;
            while (_pending.Count > 0 && created < MAX_CREATIONS_PER_FRAME &&
                   Stopwatch.GetTimestamp() - start < budget)
            {
                if (PrewarmNext(false)) created++;
            }
        }

        /// <summary>Warm the maximum per-prefab demand of any one roster unit, before battle Running.</summary>
        public async Awaitable PrewarmRosterAsync(IAssetService assets, BattleDefinition battle,
            CancellationToken cancellationToken)
        {
            WeaponsData weapons = assets.Load<WeaponsData>(nameof(WeaponsData));
            FactionCatalog factions = assets.Load<FactionCatalog>(nameof(FactionCatalog));
            ShipsData ships = assets.Load<ShipsData>(nameof(ShipsData));
            var targets = new Dictionary<ShotEffect, int>();
            var factionsSeen = new HashSet<FactionType>();
            HashSet<SquadronType> squadrons = BattleAssetPreload.Squadrons(assets, battle);
            foreach (var slot in battle.Slots)
            {
                if (!factionsSeen.Add(slot.Faction)) continue;
                foreach (var ship in factions.Get(slot.Faction).Ships.Keys)
                {
                    ShipData data = assets.Load<ShipData>(ships.GetShipDataPath(ship));
                    AddLoadout(data.WeaponLoadout, weapons, slot.Faction, targets);
                }
                foreach (SquadronType squadron in squadrons)
                {
                    SquadronData data = assets.Load<SquadronData>(squadron + "SquadronData");
                    AddLoadout(data.WeaponLoadout, weapons, slot.Faction, targets,
                        data.WarheadDamageMultiplier, data.WarheadReloadMultiplier);
                }
                foreach (string key in BattleAssetPreload.PlatformDataKeys())
                    AddLoadout(assets.Load<DefendPlatformData>(key).WeaponLoadout, weapons, slot.Faction, targets);
            }

            foreach (KeyValuePair<ShotEffect, int> target in targets)
            {
                ShotEffectCache cache = GetCache(target.Key);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Debug.Log($"[ShotPoolPrewarmer] Roster target: {target.Key.name} = {target.Value} shots.");
#endif
                cancellationToken.ThrowIfCancellationRequested();
                WarmLoadout(cache, target.Value);
            }
            await CompletePrewarmAsync(cancellationToken);
        }

        private static void AddLoadout(IReadOnlyList<WeaponLoadoutEntry> loadout, WeaponsData weapons,
            FactionType faction, Dictionary<ShotEffect, int> targets,
            float warheadDamageMultiplier = 1f, float warheadReloadMultiplier = 1f)
        {
            var unitTargets = new Dictionary<ShotEffect, int>();
            foreach (WeaponLoadoutEntry entry in loadout)
            {
                WeaponProfile profile = weapons.GetProfile(entry.WeaponType, faction)
                    .WithWarheadMultipliers(warheadDamageMultiplier, warheadReloadMultiplier);
                unitTargets.TryGetValue(profile.ShotPrefab, out int count);
                unitTargets[profile.ShotPrefab] = count + entry.Count *
                    WeaponHardPoint.ExpectedShotsInFlight(profile);
            }
            foreach (KeyValuePair<ShotEffect, int> target in unitTargets)
            {
                targets.TryGetValue(target.Key, out int current);
                targets[target.Key] = Mathf.Max(current, target.Value);
            }
        }
    }
}
