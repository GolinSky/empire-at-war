using System;
using System.Collections.Generic;
using System.Globalization;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Factions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Cheats;
using EmpireAtWar.Views.Cheats;
using Zenject;

namespace EmpireAtWar.Presenters.Cheats
{
    public sealed class CheatPresenter : IInitializable, ILateDisposable
    {
        private readonly ICheatView _view;
        private readonly ICheatService _cheatService;
        private readonly CheatWorldService _worldCheats;
        private readonly IFactionService _factionService;
        private readonly DefendPlatformCatalog _platformCatalog;
        private readonly IPlayerRoster _players;
        private readonly IPlayerRelations _relations;
        private readonly PlayerSlot _owner;
        private readonly List<PlayerSlot> _enemies = new();

        private readonly FactionCatalog _factionCatalog;
        private readonly Dictionary<ShipType, FactionData> _shipData = new();

        public CheatPresenter(
            ICheatView view,
            ICheatService cheatService,
            FactionCatalog factionCatalog,
            CheatWorldService worldCheats,
            IFactionService factionService,
            DefendPlatformCatalog platformCatalog,
            IPlayerRoster players,
            IPlayerRelations relations,
            PlayerSlot owner)
        {
            _view = view;
            _factionCatalog = factionCatalog;
            _cheatService = cheatService;
            _worldCheats = worldCheats;
            _factionService = factionService;
            _platformCatalog = platformCatalog;
            _players = players;
            _relations = relations;
            _owner = owner;
        }

        public void Initialize()
        {
            _enemies.Clear();
            foreach (PlayerSlot player in _players.Players)
            {
                if (_relations.IsHostile(_owner.Id, player.Id)) _enemies.Add(player);
            }
            _view.SetEnemies(_enemies);
            List<DefendPlatformType> platforms = new List<DefendPlatformType>(_platformCatalog.Entries.Keys);
            platforms.Sort((left, right) => ((int)left).CompareTo((int)right));
            _view.SetPlatforms(platforms);

            List<FactionType> factions = BuildShipCatalog();
            _view.SetFactions(factions);
            if (factions.Count > 0)
            {
                SelectFaction(factions[0]);
            }
            else
            {
                _view.SetShips(Array.Empty<ShipType>());
            }

            _view.FactionSelected += SelectFaction;
            _view.AddMoneyRequested += AddMoney;
            _view.AddReinforcementRequested += AddReinforcement;
            _view.SpawnForceRequested += SpawnForce;
            _view.GrantSuperWeaponRequested += GrantSuperWeapon;
            _view.GrantAllSuperWeaponsRequested += GrantAllSuperWeapons;
            _view.RangeDebugToggled += SetRangeDebug;
            _view.DestroyOwnShipsRequested += DestroyOwnShips;
            _view.LevelUpStationRequested += LevelUpStation;
            _view.SpawnAllShipsRequested += SpawnAllShips;
            _view.AddPlatformReinforcementRequested += AddPlatformReinforcement;
            _view.SpawnPlatformRequested += SpawnPlatform;
            _view.AddEnemyMoneyRequested += AddEnemyMoney;
            _view.AddAllEnemiesMoneyRequested += AddAllEnemiesMoney;
        }

        public void LateDispose()
        {
            _view.FactionSelected -= SelectFaction;
            _view.AddMoneyRequested -= AddMoney;
            _view.AddReinforcementRequested -= AddReinforcement;
            _view.SpawnForceRequested -= SpawnForce;
            _view.GrantSuperWeaponRequested -= GrantSuperWeapon;
            _view.GrantAllSuperWeaponsRequested -= GrantAllSuperWeapons;
            _view.RangeDebugToggled -= SetRangeDebug;
            _view.DestroyOwnShipsRequested -= DestroyOwnShips;
            _view.LevelUpStationRequested -= LevelUpStation;
            _view.SpawnAllShipsRequested -= SpawnAllShips;
            _view.AddPlatformReinforcementRequested -= AddPlatformReinforcement;
            _view.SpawnPlatformRequested -= SpawnPlatform;
            _view.AddEnemyMoneyRequested -= AddEnemyMoney;
            _view.AddAllEnemiesMoneyRequested -= AddAllEnemiesMoney;
        }

        private List<FactionType> BuildShipCatalog()
        {
            _shipData.Clear();
            List<FactionType> factions = new List<FactionType>();
            foreach (FactionDefinition faction in _factionCatalog.Factions)
            {
                factions.Add(faction.FactionType);
                foreach (KeyValuePair<ShipType, FactionData> ship in faction.Ships)
                {
                    if (_shipData.ContainsKey(ship.Key))
                    {
                        throw new InvalidOperationException(
                            $"Ship {ship.Key} is configured for more than one faction.");
                    }

                    _shipData.Add(ship.Key, ship.Value);
                }
            }

            return factions;
        }

        private void SelectFaction(FactionType factionType)
        {
            List<ShipType> ships = new List<ShipType>(_factionCatalog.Get(factionType).Ships.Keys);
            ships.Sort((left, right) => ((int)left).CompareTo((int)right));
            _view.SetShips(ships);
        }

        private void AddMoney(string value)
        {
            if (!TryParseMoney(value, out float amount)) return;
            _cheatService.AddMoney(amount);
            _view.SetStatus($"Added {amount:0.##} money.");
        }

        private void AddReinforcement(ShipType shipType)
        {
            _cheatService.AddShipReinforcement(CreateRequest(shipType));
            _view.SetStatus($"Added {shipType} to reinforcement.");
        }

        private void SpawnForce(ShipType shipType)
        {
            bool spawned = _cheatService.ForceSpawnShipAtDefaultZone(CreateRequest(shipType));
            _view.SetStatus(spawned
                ? $"Spawned {shipType} at the default zone."
                : "No player-owned reinforcement zone is available.");
        }

        private void GrantSuperWeapon(SuperWeaponType type)
        {
            _view.SetStatus(_cheatService.GrantSuperWeapon(type)
                ? $"{type} is ready to fire."
                : $"{type} is already charging or ready.");
        }

        private void GrantAllSuperWeapons()
        {
            Array types = Enum.GetValues(typeof(SuperWeaponType));
            int granted = 0;
            foreach (SuperWeaponType type in types)
            {
                if (_cheatService.GrantSuperWeapon(type)) granted++;
            }

            _view.SetStatus(granted == types.Length
                ? "All superweapons are ready to fire."
                : $"Granted {granted} of {types.Length}; the rest were already charging or ready.");
        }

        private void SetRangeDebug(bool isEnabled)
        {
            _cheatService.SetRangeDebug(isEnabled);
            _view.SetStatus(isEnabled
                ? "Range debug on: select units to see attack (red) and radar (blue) range."
                : "Range debug off.");
        }

        private void DestroyOwnShips()
        {
            int destroyed = _cheatService.DestroyOwnShips();
            _view.SetStatus(destroyed > 0
                ? $"Destroyed {destroyed} of your ships."
                : "You have no ships to destroy.");
        }

        private void LevelUpStation()
        {
            _view.SetStatus(_factionService.ForceUpgradeStation()
                ? "Station upgraded by one level. Queued upgrades were refunded."
                : "Station is at maximum level or no production base is available.");
        }

        private void SpawnAllShips(FactionType faction)
        {
            int spawned = 0;
            Dictionary<ShipType, FactionData> ships = _factionCatalog.Get(faction).Ships;
            foreach (ShipType type in ships.Keys)
            {
                if (_cheatService.ForceSpawnShipAtDefaultZone(CreateRequest(type))) spawned++;
            }
            _view.SetStatus($"Spawned {spawned} of {ships.Count} {faction} ships; others had no valid spawn position.");
        }

        private void AddPlatformReinforcement(DefendPlatformType type)
        {
            _worldCheats.AddPlatformReinforcement(new DefendPlatformUnitRequest(_platformCatalog.Get(type), type));
            _view.SetStatus($"Added {type} to reinforcement.");
        }

        private void SpawnPlatform(DefendPlatformType type)
        {
            bool spawned = _worldCheats.ForceSpawnPlatform(
                new DefendPlatformUnitRequest(_platformCatalog.Get(type), type));
            _view.SetStatus(spawned ? $"Spawned {type} in the home area."
                : "No valid defense platform position is available in the home area.");
        }

        private bool TryParseMoney(string value, out float amount)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out amount)
                && amount > 0f && !float.IsNaN(amount) && !float.IsInfinity(amount)) return true;

            _view.SetStatus("Enter a positive, finite money amount.");
            return false;
        }

        private void AddEnemyMoney(PlayerId enemy, string value)
        {
            if (!TryParseMoney(value, out float amount)) return;
            _worldCheats.AddEnemyMoney(enemy, amount);
            _view.SetStatus($"Added {amount:0.##} money to {enemy}.");
        }

        private void AddAllEnemiesMoney(string value)
        {
            if (!TryParseMoney(value, out float amount)) return;
            foreach (PlayerSlot enemy in _enemies) _worldCheats.AddEnemyMoney(enemy.Id, amount);
            _view.SetStatus($"Added {amount:0.##} money to each of {_enemies.Count} enemies.");
        }

        private ShipUnitRequest CreateRequest(ShipType shipType)
        {
            if (!_shipData.TryGetValue(shipType, out FactionData factionData))
            {
                throw new InvalidOperationException($"Ship {shipType} has no faction data.");
            }

            return new ShipUnitRequest(factionData, shipType);
        }
    }
}
