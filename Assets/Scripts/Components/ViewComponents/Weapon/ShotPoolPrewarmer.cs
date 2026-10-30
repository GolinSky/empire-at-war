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
        private const int MAX_CREATIONS_PER_FRAME = 1;

        private readonly Queue<(ShotEffectCache Cache, int Target)> _pending =
            new Queue<(ShotEffectCache, int)>();
        private readonly Dictionary<ShotEffect, ShotEffectCache> _caches =
            new Dictionary<ShotEffect, ShotEffectCache>();
        private readonly Dictionary<ShotEffectCache, int> _targets = new Dictionary<ShotEffectCache, int>();
        private readonly HashSet<ShotEffectPool> _pools = new HashSet<ShotEffectPool>();

        public ShotEffectPool CreatePool(WeaponProfile profile, Transform owner, Action<int> completed,
            ImpactEffectPresenter impacts, BoltFlightBatch flights, int target)
        {
            ShotEffectCache cache = GetCache(profile.ShotPrefab);
            var pool = new ShotEffectPool(profile, owner, completed, impacts, flights, cache);
            _pools.Add(pool);
            pool.Retired += OnRetired;
            Enqueue(cache, target);
            return pool;
        }

        private ShotEffectCache GetCache(ShotEffect prefab)
        {
            if (!_caches.TryGetValue(prefab, out ShotEffectCache cache))
            {
                cache = new ShotEffectCache(prefab);
                _caches.Add(prefab, cache);
            }
            return cache;
        }

        private void Enqueue(ShotEffectCache cache, int target)
        {
            if (_targets.TryGetValue(cache, out int current) && current >= target) return;
            _targets[cache] = target;
            _pending.Enqueue((cache, target));
        }

        public void RequestPrewarm(IReadOnlyList<WeaponProfile> profiles)
        {
            var targets = new Dictionary<ShotEffect, int>();
            foreach (WeaponProfile profile in profiles)
            {
                targets.TryGetValue(profile.ShotPrefab, out int count);
                targets[profile.ShotPrefab] = count + WeaponHardPoint.ExpectedShotsInFlight(profile);
            }
            foreach (KeyValuePair<ShotEffect, int> target in targets) Enqueue(GetCache(target.Key), target.Value);
        }

        public async Awaitable CompletePrewarmAsync(CancellationToken cancellationToken)
        {
            while (_pending.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                (ShotEffectCache cache, int target) = _pending.Peek();
                if (cache.PrewarmOne(target)) await Awaitable.NextFrameAsync(cancellationToken);
                else _pending.Dequeue();
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
            _targets.Clear();
            foreach (ShotEffectPool pool in new List<ShotEffectPool>(_pools)) pool.DestroyAll();
            foreach (ShotEffectCache cache in _caches.Values) cache.DestroyAll();
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
                (ShotEffectCache cache, int target) = _pending.Peek();
                if (cache.PrewarmOne(target)) created++;
                else _pending.Dequeue();
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
                    AddLoadout(assets.Load<ShipData>(ships.GetShipDataPath(ship)).WeaponLoadout, weapons, slot.Faction, targets);
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
                while (cache.PrewarmOne(target.Value))
                    await Awaitable.NextFrameAsync(cancellationToken);
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
                unitTargets[profile.ShotPrefab] = count + entry.Count * WeaponHardPoint.ExpectedShotsInFlight(profile);
            }
            foreach (KeyValuePair<ShotEffect, int> target in unitTargets)
            {
                targets.TryGetValue(target.Key, out int current);
                targets[target.Key] = Mathf.Max(current, target.Value);
            }
        }
    }
}
