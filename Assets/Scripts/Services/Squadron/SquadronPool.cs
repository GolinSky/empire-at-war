using System.Collections.Generic;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Squadrons
{
    /// <summary>
    /// Reuses destroyed squadrons instead of building a new squadron context per launch, which costs several
    /// milliseconds. Owner and type change the installed bindings, so squadrons are pooled per owner and type.
    /// Despawned squadrons are destroyed with the battle scene. Every type the owner's faction can launch is
    /// built while the battle loads, so early launches do not pay for a new context mid-battle.
    /// </summary>
    public sealed class SquadronPool : ISquadronSpawner, IInitializable, ITickable
    {
        // How long a destroyed squadron's wrecks and explosions stay in the battle before it is despawned.
        private const float DEATH_PRESENTATION_SECONDS = 6f;
        private const int PREWARM_PER_TYPE = 4;
        // Far below the battle plane: prewarmed squadrons are stowed in the frame they are built.
        private static readonly Vector3 PREWARM_POSITION = new Vector3(0f, -10000f, 0f);

        private readonly SquadronFactory _factory;
        private readonly PlayerSlot _owner;
        private readonly FactionCatalog _factions;
        private readonly ShipsData _shipsData;
        private readonly IAssetService _assetService;
        private readonly Dictionary<(PlayerId, SquadronType), List<Squadron>> _despawned =
            new Dictionary<(PlayerId, SquadronType), List<Squadron>>();
        private readonly List<DyingSquadron> _dying = new List<DyingSquadron>();

        public SquadronPool(SquadronFactory factory, PlayerSlot owner, FactionCatalog factions, ShipsData shipsData,
            IAssetService assetService)
        {
            _factory = factory;
            _owner = owner;
            _factions = factions;
            _shipsData = shipsData;
            _assetService = assetService;
        }

        public void Initialize()
        {
            foreach (SquadronType squadronType in CollectLaunchableTypes())
            {
                List<Squadron> squadrons = new List<Squadron>(PREWARM_PER_TYPE);
                for (int i = 0; i < PREWARM_PER_TYPE; i++)
                {
                    Squadron squadron = _factory.Create(_owner.Id, squadronType, PREWARM_POSITION, Quaternion.identity);
                    squadron.Stow();
                    squadrons.Add(squadron);
                }

                _despawned.Add((_owner.Id, squadronType), squadrons);
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

        // Buildable squadrons plus every hangar bay of the faction's ships and space station.
        private HashSet<SquadronType> CollectLaunchableTypes()
        {
            FactionDefinition faction = _factions.Get(_owner.Faction);
            HashSet<SquadronType> types = new HashSet<SquadronType>(faction.Squadrons.Keys);
            foreach (var shipType in faction.Ships.Keys)
            {
                ShipData ship = _assetService.Load<ShipData>(_shipsData.GetShipDataPath(shipType));
                foreach (HangarBay bay in ship.HangarBays) types.Add(bay.SquadronType);
            }

            types.Add(_assetService.Load<SpaceStationData>(nameof(SpaceStationData)).GetHangarBay(_owner.Faction).SquadronType);
            return types;
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
