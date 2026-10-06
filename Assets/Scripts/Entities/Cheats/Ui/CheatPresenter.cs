using System;
using System.Collections.Generic;
using System.Globalization;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.SuperWeapons;
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

        private readonly FactionCatalog _factionCatalog;
        private readonly Dictionary<ShipType, FactionData> _shipData = new();

        public CheatPresenter(
            ICheatView view,
            ICheatService cheatService,
            FactionCatalog factionCatalog)
        {
            _view = view;
            _factionCatalog = factionCatalog;
            _cheatService = cheatService;
        }

        public void Initialize()
        {
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
            bool parsed = float.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float amount);
            if (!parsed || amount <= 0f)
            {
                _view.SetStatus("Enter a positive money amount.");
                return;
            }

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
