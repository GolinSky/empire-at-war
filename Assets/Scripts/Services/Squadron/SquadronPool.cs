using System.Collections.Generic;
using System.Threading;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Player;
using Unity.Profiling;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Services.Squadrons
{
    /// <summary>
    /// Reuses destroyed squadrons instead of building a new squadron context per launch, which costs several
    /// milliseconds. Owner and type change the installed bindings, so squadrons are pooled per owner and type.
    /// Despawned squadrons are destroyed with the battle scene. Every type the owner's faction can launch is
    /// built while the battle loads, so early launches do not pay for a new context mid-battle; types launched
    /// by many hangars get more.
    /// </summary>
    public sealed class SquadronPool : ISquadronSpawner, ISquadronPrewarmer, IInitializable, ITickable, ILateDisposable
    {
        // How long a destroyed squadron's wrecks and explosions stay in the battle before it is despawned.
        private const float DEATH_PRESENTATION_SECONDS = 6f;
        // Buildable squadrons come from production rather than a hangar bay.
        private const int BUILDABLE_PREWARM = 2;
        private const int MAX_PREWARM_PER_TYPE = 12;
        // Far below the battle plane: prewarmed squadrons are stowed in the frame they are built.
        private static readonly Vector3 PREWARM_POSITION = new Vector3(0f, -10000f, 0f);
        private static readonly ProfilerMarker INITIALIZE_MARKER = new ProfilerMarker("SquadronPool.Initialize");
        private static readonly ProfilerMarker COUNT_PREWARM_MARKER = new ProfilerMarker("SquadronPool.CountPrewarm");
        private static readonly ProfilerMarker PREWARM_ITEM_MARKER = new ProfilerMarker("SquadronPool.PrewarmItem");

        private readonly SquadronFactory _factory;
        private readonly PlayerSlot _owner;
        private readonly FactionCatalog _factions;
        private readonly ShipsData _shipsData;
        private readonly IAssetService _assetService;
        private readonly IPlayerRegistry _playerRegistry;
        private readonly Dictionary<(PlayerId, SquadronType), List<Squadron>> _despawned =
            new Dictionary<(PlayerId, SquadronType), List<Squadron>>();
        private readonly List<DyingSquadron> _dying = new List<DyingSquadron>();

        public SquadronPool(SquadronFactory factory, PlayerSlot owner, FactionCatalog factions, ShipsData shipsData,
            IAssetService assetService, IPlayerRegistry playerRegistry)
        {
            _factory = factory;
            _owner = owner;
            _factions = factions;
            _shipsData = shipsData;
            _assetService = assetService;
            _playerRegistry = playerRegistry;
        }

        public void Initialize()
        {
            using (INITIALIZE_MARKER.Auto())
                _playerRegistry.RegisterSquadronPrewarmer(_owner.Id, this);
        }

        public void LateDispose() => _playerRegistry.UnregisterSquadronPrewarmer(_owner.Id);

        public async Awaitable PrewarmAsync(CancellationToken cancellationToken)
        {
            using (IEnumerator<Squadron> prewarm = CreatePrewarmed(cancellationToken).GetEnumerator())
            {
                while (prewarm.MoveNext())
                    await Awaitable.NextFrameAsync(cancellationToken);
            }
        }

        private IEnumerable<Squadron> CreatePrewarmed(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (KeyValuePair<SquadronType, int> prewarm in CountPrewarm())
            {
                SquadronType squadronType = prewarm.Key;
                int count = Mathf.Min(prewarm.Value, MAX_PREWARM_PER_TYPE);
                (PlayerId, SquadronType) key = (_owner.Id, squadronType);
                if (!_despawned.TryGetValue(key, out List<Squadron> squadrons))
                {
                    squadrons = new List<Squadron>(count);
                    _despawned.Add(key, squadrons);
                }

                for (int index = 0; index < count; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Squadron squadron;
                    using (PREWARM_ITEM_MARKER.Auto())
                    {
                        squadron = _factory.Create(_owner.Id, squadronType, PREWARM_POSITION, Quaternion.identity);
                        squadron.Stow();
                        squadrons.Add(squadron);
                    }
                    yield return squadron;
                }
            }
        }

        public Squadron Spawn(PlayerId owner, SquadronType squadronType, Vector3 position, Quaternion rotation)
        {
            (PlayerId, SquadronType) key = (owner, squadronType);
            Squadron squadron;
            if (TryTakeDespawned(key, out squadron))
            {
                squadron.Respawn(position, rotation);
            }
            else
            {
                squadron = _factory.Create(owner, squadronType, position, rotation);
            }

            // Respawning drops the previous life's listeners, so every life is watched anew.
            squadron.Released += () => HandleReleased(squadron, key);
            return squadron;
        }

        public Squadron SpawnRestored(PlayerId owner, SquadronType squadronType, Vector3 position, Quaternion rotation,
            long entityId)
        {
            (PlayerId, SquadronType) key = (owner, squadronType);
            Squadron squadron = _factory.Create(owner, squadronType, position, rotation, entityId);
            squadron.Released += () => HandleReleased(squadron, key);
            return squadron;
        }

        public void Tick()
        {
            for (int i = _dying.Count - 1; i >= 0; i--)
            {
                DyingSquadron dying = _dying[i];
                if (Time.time < dying.DespawnTime) continue;
                _dying.RemoveAt(i);
                dying.Squadron.Despawn();
                if (!_despawned.TryGetValue(dying.Key, out List<Squadron> squadrons))
                {
                    squadrons = new List<Squadron>();
                    _despawned.Add(dying.Key, squadrons);
                }

                squadrons.Add(dying.Squadron);
            }
        }

        // A squadron released without being destroyed is leaving with the scene and is not reused.
        private void HandleReleased(Squadron squadron, (PlayerId, SquadronType) key)
        {
            if (!squadron.IsDestroyed) return;
            _dying.Add(new DyingSquadron
            {
                Squadron = squadron,
                Key = key,
                DespawnTime = Time.time + DEATH_PRESENTATION_SECONDS
            });
        }

        // Per type: a few for production when buildable, plus the active limit of every hangar bay of the faction's
        // ships and space station that launches it.
        private Dictionary<SquadronType, int> CountPrewarm()
        {
            using (COUNT_PREWARM_MARKER.Auto())
            {
                FactionDefinition faction = _factions.Get(_owner.Faction);
                Dictionary<SquadronType, int> counts = new Dictionary<SquadronType, int>();
                foreach (SquadronType squadronType in faction.Squadrons.Keys) counts[squadronType] = BUILDABLE_PREWARM;
                foreach (var shipType in faction.Ships.Keys)
                {
                    ShipData ship = _assetService.Load<ShipData>(_shipsData.GetShipDataPath(shipType));
                    foreach (HangarBay bay in ship.HangarBays) AddBay(counts, bay);
                }

                AddBay(counts, _assetService.Load<SpaceStationData>(nameof(SpaceStationData)).GetHangarBay(_owner.Faction));
                return counts;
            }
        }

        private static void AddBay(Dictionary<SquadronType, int> counts, HangarBay bay)
        {
            counts.TryGetValue(bay.SquadronType, out int count);
            counts[bay.SquadronType] = count + bay.MaxActive;
        }

        private bool TryTakeDespawned((PlayerId, SquadronType) key, out Squadron squadron)
        {
            squadron = null;
            if (!_despawned.TryGetValue(key, out List<Squadron> squadrons)) return false;
            for (int i = 0; i < squadrons.Count; i++)
            {
                if (!squadrons[i].CanRespawn) continue;
                squadron = squadrons[i];
                squadrons.RemoveAt(i);
                return true;
            }

            return false;
        }

        private struct DyingSquadron
        {
            public Squadron Squadron;
            public (PlayerId, SquadronType) Key;
            public float DespawnTime;
        }
    }
}
