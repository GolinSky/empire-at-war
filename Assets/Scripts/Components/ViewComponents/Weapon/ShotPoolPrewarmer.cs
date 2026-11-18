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

        private readonly List<ShotEffectCache> _pending = new List<ShotEffectCache>();
        private readonly HashSet<ShotEffectCache> _queued = new HashSet<ShotEffectCache>();
        // Fighters and ships get separate caches (and admission queues) of the same shot prefab.
        private readonly Dictionary<(ShotEffect Prefab, bool Strikecraft), ShotEffectCache> _caches =
            new Dictionary<(ShotEffect, bool), ShotEffectCache>();
        private readonly Dictionary<ShotEffectCache, int> _targets = new Dictionary<ShotEffectCache, int>();
        private readonly Dictionary<ShotEffectPool, (ShotEffectCache Cache, int Capacity)> _pools =
            new Dictionary<ShotEffectPool, (ShotEffectCache, int)>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public IEnumerable<ShotEffectCache> Caches => _caches.Values;
#endif

        public ShotEffectPool CreatePool(WeaponProfile profile, Transform owner, Action<int> completed,
            ImpactEffectPresenter impacts, BoltFlightBatch flights, int target, bool strikecraft = false)
        {
            ShotEffectCache cache = GetCache(profile.ShotPrefab, strikecraft);
            cache.RegisterCapacity(target);
            // Squadrons are built before battle: their summed capacity is the expected live fighter demand.
            if (strikecraft) WarmLoadout(cache, cache.RegisteredCapacity);
            var pool = new ShotEffectPool(profile, owner, completed, impacts, flights, cache, target);
            _pools.Add(pool, (cache, target));
            pool.Retired += OnRetired;
            return pool;
        }

        internal void IncreaseCapacity(ShotEffectPool pool, int capacity)
        {
            (ShotEffectCache cache, int previousCapacity) = _pools[pool];
            cache.RegisterCapacity(capacity - previousCapacity);
            if (cache.IsStrikecraftGroup) WarmLoadout(cache, cache.RegisteredCapacity);
            _pools[pool] = (cache, capacity);
            pool.IncreaseCapacity(capacity);
        }

        private ShotEffectCache GetCache(ShotEffect prefab, bool strikecraft)
        {
            if (!_caches.TryGetValue((prefab, strikecraft), out ShotEffectCache cache))
            {
                cache = new ShotEffectCache(prefab, strikecraft);
                cache.DemandChanged += Enqueue;
                _caches.Add((prefab, strikecraft), cache);
                _targets.Add(cache, 0);
            }
            return cache;
        }

        private void Enqueue(ShotEffectCache cache)
        {
            if (_queued.Add(cache)) _pending.Add(cache);
        }

        public void RequestPrewarm(IReadOnlyList<WeaponProfile> profiles, bool strikecraft = false)
        {
            var targets = new Dictionary<ShotEffect, int>();
            foreach (WeaponProfile profile in profiles)
            {
                targets.TryGetValue(profile.ShotPrefab, out int count);
                targets[profile.ShotPrefab] = count + WeaponHardPoint.ExpectedShotsInFlight(profile);
            }
            foreach (KeyValuePair<ShotEffect, int> target in targets) WarmLoadout(GetCache(target.Key, strikecraft), target.Value);
        }

        private void WarmLoadout(ShotEffectCache cache, int target)
        {
            _targets[cache] = Math.Max(_targets[cache], target);
            Enqueue(cache);
        }

        private bool PrewarmNext(bool loading)
        {
            // The cache furthest below its target (waiting salvos included) grows first, not round-robin.
            int best = 0;
            int bestDeficit = int.MinValue;
            for (int i = 0; i < _pending.Count; i++)
            {
                int deficit = Target(_pending[i]) - _pending[i].TotalCount;
                if (deficit <= bestDeficit) continue;
                best = i;
                bestDeficit = deficit;
            }
            ShotEffectCache cache = _pending[best];
            int target = Target(cache);
            bool created = cache.PrewarmOne(target, loading);
            if (!created || cache.TotalCount >= target)
            {
                _pending[best] = _pending[_pending.Count - 1];
                _pending.RemoveAt(_pending.Count - 1);
                _queued.Remove(cache);
            }
            return created;
        }

        private int Target(ShotEffectCache cache) => Math.Max(_targets[cache], cache.RequiredCount);

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
            while (_pending.Count > 0 && Stopwatch.GetTimestamp() - start < budget)
                PrewarmNext(false);
        }

        /// <summary>Warm roster ship demand (summed) and the largest fighter demand, before battle Running.</summary>
        public async Awaitable PrewarmRosterAsync(IAssetService assets, BattleDefinition battle,
            CancellationToken cancellationToken)
        {
            WeaponsData weapons = assets.Load<WeaponsData>(nameof(WeaponsData));
            FactionCatalog factions = assets.Load<FactionCatalog>(nameof(FactionCatalog));
            ShipsData ships = assets.Load<ShipsData>(nameof(ShipsData));
            // Ships: every player fields a fleet, so each roster ship and platform adds its own demand.
            // Fighters: the largest single squadron here; squadron pools add their summed demand when they are built.
            var shipTargets = new Dictionary<ShotEffect, int>();
            var strikecraftTargets = new Dictionary<ShotEffect, int>();
            HashSet<SquadronType> squadrons = BattleAssetPreload.Squadrons(assets, battle);
            foreach (var slot in battle.Slots)
            {
                foreach (var ship in factions.Get(slot.Faction).Ships.Keys)
                {
                    ShipData data = assets.Load<ShipData>(ships.GetShipDataPath(ship));
                    AddLoadout(data.WeaponLoadout, weapons, slot.Faction, shipTargets, true);
                }
                foreach (SquadronType squadron in squadrons)
                {
                    SquadronData data = assets.Load<SquadronData>(squadron + "SquadronData");
                    AddLoadout(data.WeaponLoadout, weapons, slot.Faction, strikecraftTargets, false,
                        data.WarheadDamageMultiplier, data.WarheadReloadMultiplier);
                }
                foreach (string key in BattleAssetPreload.PlatformDataKeys())
                    AddLoadout(assets.Load<DefendPlatformData>(key).WeaponLoadout, weapons, slot.Faction, shipTargets, true);
            }

            WarmRoster(shipTargets, false, cancellationToken);
            WarmRoster(strikecraftTargets, true, cancellationToken);
            await CompletePrewarmAsync(cancellationToken);
        }

        private void WarmRoster(Dictionary<ShotEffect, int> targets, bool strikecraft,
            CancellationToken cancellationToken)
        {
            foreach (KeyValuePair<ShotEffect, int> target in targets)
            {
                ShotEffectCache cache = GetCache(target.Key, strikecraft);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Debug.Log($"[ShotPoolPrewarmer] Roster target: {target.Key.name} ({(strikecraft ? "fighters" : "ships")}) = {target.Value} shots.");
#endif
                cancellationToken.ThrowIfCancellationRequested();
                WarmLoadout(cache, target.Value);
            }
        }

        private static void AddLoadout(IReadOnlyList<WeaponLoadoutEntry> loadout, WeaponsData weapons,
            FactionType faction, Dictionary<ShotEffect, int> targets, bool accumulate,
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
                targets[target.Key] = accumulate ? current + target.Value : Mathf.Max(current, target.Value);
            }
        }
    }
}
