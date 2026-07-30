using System;
using System.Collections.Generic;
using System.Globalization;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Cheats;
using EmpireAtWar.Views.Cheats;
using Zenject;

namespace EmpireAtWar.Presenters.Cheats
{
    public sealed class CheatPresenter : IInitializable, ILateDisposable
    {
        private readonly ICheatView _view;
        private readonly FactionsModel _factionsModel;
        private readonly ICheatService _cheatService;
        private readonly Dictionary<ShipType, FactionData> _shipData = new();

        public CheatPresenter(
            ICheatView view,
            FactionsModel factionsModel,
            ICheatService cheatService)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _factionsModel = factionsModel ?? throw new ArgumentNullException(nameof(factionsModel));
            _cheatService = cheatService ?? throw new ArgumentNullException(nameof(cheatService));
        }

        public void Initialize()
        {
            List<ShipType> ships = BuildShipCatalog();
            _view.SetShips(ships);
            _view.AddMoneyRequested += AddMoney;
            _view.AddReinforcementRequested += AddReinforcement;
            _view.SpawnForceRequested += SpawnForce;
        }

        public void LateDispose()
        {
            _view.AddMoneyRequested -= AddMoney;
            _view.AddReinforcementRequested -= AddReinforcement;
            _view.SpawnForceRequested -= SpawnForce;
        }

        private List<ShipType> BuildShipCatalog()
        {
            _shipData.Clear();
            foreach (FactionType factionType in Enum.GetValues(typeof(FactionType)))
            {
                foreach (KeyValuePair<ShipType, FactionData> ship in
                         _factionsModel.GetShipFactionData(factionType))
                {
                    if (_shipData.ContainsKey(ship.Key))
                    {
                        throw new InvalidOperationException(
                            $"Ship {ship.Key} is configured for more than one faction.");
                    }

                    _shipData.Add(ship.Key, ship.Value);
                }
            }

            List<ShipType> ships = new List<ShipType>(_shipData.Keys);
            ships.Sort((left, right) => ((int)left).CompareTo((int)right));
            return ships;
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
