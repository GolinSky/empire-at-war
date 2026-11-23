using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.SpaceStation;
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
    /// Hands each mount its shot visuals and owns per-prefab battle storage. Bolts draw through
    /// <see cref="BoltFlightBatch"/> with no per-shot object; other shots lease pooled visuals. Loading warms roster
    /// demand; runtime growth is budgeted per frame, with a larger budget while shots are firing undrawn.
    /// </summary>
    public sealed class ShotPoolPrewarmer : ITickable, ILateDisposable
    {
        private const double BUDGET_MILLISECONDS = 0.5;
        private const double DEMAND_BUDGET_MILLISECONDS = 2.0;
        private const double LOADING_BUDGET_MILLISECONDS = 4.0;

        private readonly BoltFlightBatch _bolts;

        private readonly List<ShotEffectCache> _pending = new List<ShotEffectCache>();
        private readonly HashSet<ShotEffectCache> _queued = new HashSet<ShotEffectCache>();
        // Fighters and ships get separate caches (and admission queues) of the same shot prefab.
        private readonly Dictionary<(ShotEffect Prefab, bool Strikecraft), ShotEffectCache> _caches =
            new Dictionary<(ShotEffect, bool), ShotEffectCache>();
        private readonly Dictionary<ShotEffectCache, int> _targets = new Dictionary<ShotEffectCache, int>();
        private readonly HashSet<ShotEffectPool> _pools = new HashSet<ShotEffectPool>();
        public Dictionary<(ShotEffect Prefab, bool Strikecraft), ShotEffectCache>.ValueCollection Caches => _caches.Values;

        public ShotPoolPrewarmer(BoltFlightBatch bolts)
        {
            _bolts = bolts;
        }

        /// <param name="capacity">Shots this mount expects in flight; pre-sizes its lease set.</param>
        public IShotVisuals CreateVisuals(WeaponProfile profile, Transform owner, ImpactEffectPresenter impacts,
            int capacity, bool strikecraft = false)
        {
            if (profile.ShotPrefab is BoltShot bolt) return new BoltShotVisuals(_bolts, bolt, profile, owner, impacts);
            var pool = new ShotEffectPool(profile, owner, impacts, GetCache(profile.ShotPrefab, strikecraft), capacity);
            _pools.Add(pool);
            pool.Retired += OnRetired;
            return pool;
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
                if (profile.ShotPrefab is BoltShot bolt)
                {
                    _bolts.Prepare(bolt);
                    continue;
                }
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
            _pools.Remove(pool);
        }

        public void Clear()
        {
            _pending.Clear();
            _queued.Clear();
            _targets.Clear();
            foreach (ShotEffectPool pool in new List<ShotEffectPool>(_pools)) pool.DestroyAll();
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
            long budget = (long)(GetBudgetMilliseconds() * Stopwatch.Frequency / 1000.0);
            while (_pending.Count > 0 && Stopwatch.GetTimestamp() - start < budget)
                PrewarmNext(false);
        }

        // Shots firing undrawn grow their storage faster; plain stock top-ups keep the small budget.
        private double GetBudgetMilliseconds()
        {
            foreach (ShotEffectCache cache in _pending)
                if (cache.PendingDemand > 0) return DEMAND_BUDGET_MILLISECONDS;
            return BUDGET_MILLISECONDS;
        }

        /// <summary>Warm ship and station-fighter stock sized from the battle's slots, before battle Running.</summary>
        public async Awaitable PrewarmRosterAsync(IAssetService assets, BattleDefinition battle,
            CancellationToken cancellationToken)
        {
            WeaponsData weapons = assets.Load<WeaponsData>(nameof(WeaponsData));
            FactionCatalog factions = assets.Load<FactionCatalog>(nameof(FactionCatalog));
            ShipsData ships = assets.Load<ShipsData>(nameof(ShipsData));
            SpaceStationData stations = assets.Load<SpaceStationData>(nameof(SpaceStationData));
            var slotsPerFaction = new Dictionary<FactionType, int>();
            foreach (var slot in battle.Slots)
            {
                slotsPerFaction.TryGetValue(slot.Faction, out int slots);
                slotsPerFaction[slot.Faction] = slots + 1;
            }

            // Ships: one of every faction ship and platform per slot using that faction.
            Dictionary<ShotEffect, int> shipTargets = BuildStock(slotsPerFaction, faction =>
            {
                var demand = new Dictionary<ShotEffect, int>();
                foreach (var ship in factions.Get(faction).Ships.Keys)
                {
                    ShipData data = assets.Load<ShipData>(ships.GetShipDataPath(ship));
                    AddLoadout(data.WeaponLoadout, weapons, faction, demand);
                }
                foreach (string key in BattleAssetPreload.PlatformDataKeys())
                    AddLoadout(assets.Load<DefendPlatformData>(key).WeaponLoadout, weapons, faction, demand);
                return demand;
            });
            // Fighters: the station's starting squadrons, each squadron's full loadout (all its fighters).
            Dictionary<ShotEffect, int> strikecraftTargets = BuildStock(slotsPerFaction, faction =>
            {
                var demand = new Dictionary<ShotEffect, int>();
                HangarBay bay = stations.GetHangarBay(faction);
                SquadronData data = assets.Load<SquadronData>(bay.SquadronType + "SquadronData");
                var squadron = new Dictionary<ShotEffect, int>();
                AddLoadout(data.WeaponLoadout, weapons, faction, squadron,
                    data.WarheadDamageMultiplier, data.WarheadReloadMultiplier);
                foreach (KeyValuePair<ShotEffect, int> entry in squadron) demand[entry.Key] = entry.Value * bay.MaxActive;
                return demand;
            });

            WarmRoster(shipTargets, false, cancellationToken);
            WarmRoster(strikecraftTargets, true, cancellationToken);
            await CompletePrewarmAsync(cancellationToken);
        }

        /// <summary>Per-faction demand scaled by how many slots use that faction, summed per shot prefab.</summary>
        internal static Dictionary<ShotEffect, int> BuildStock(IReadOnlyDictionary<FactionType, int> slotsPerFaction,
            Func<FactionType, Dictionary<ShotEffect, int>> factionDemand)
        {
            var stock = new Dictionary<ShotEffect, int>();
            foreach (KeyValuePair<FactionType, int> faction in slotsPerFaction)
            {
                foreach (KeyValuePair<ShotEffect, int> demand in factionDemand(faction.Key))
                {
                    stock.TryGetValue(demand.Key, out int current);
                    stock[demand.Key] = current + demand.Value * faction.Value;
                }
            }
            return stock;
        }

        private void WarmRoster(Dictionary<ShotEffect, int> targets, bool strikecraft,
            CancellationToken cancellationToken)
        {
            foreach (KeyValuePair<ShotEffect, int> target in targets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (target.Key is BoltShot bolt)
                {
                    _bolts.Prepare(bolt);
                    continue;
                }
                ShotEffectCache cache = GetCache(target.Key, strikecraft);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Debug.Log($"[ShotPoolPrewarmer] Roster target: {target.Key.name} ({(strikecraft ? "fighters" : "ships")}) = {target.Value} shots.");
#endif
                WarmLoadout(cache, target.Value);
            }
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
                targets[target.Key] = current + target.Value;
            }
        }
    }
}
