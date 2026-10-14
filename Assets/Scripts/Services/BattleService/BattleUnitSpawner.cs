using System.Collections.Generic;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.Ship;
using UnityEngine;

namespace EmpireAtWar.Services.Battle
{
    public sealed class BattleUnitSpawner
    {
        private readonly Dictionary<PlayerId, (ShipFactory Ships, ISquadronSpawner Squadrons)> _players = new();
        public void Register(PlayerId owner, ShipFactory ships, ISquadronSpawner squadrons) =>
            _players.Add(owner, (ships, squadrons));
        public void Clear() => _players.Clear();

        public void Spawn(PlayerId owner, UnitTypeId unit, Vector3 position)
        {
            var factories = _players[owner];
            if (unit.IsShip) factories.Ships.Create(owner, unit.ShipType, position);
            else factories.Squadrons.Spawn(owner, unit.SquadronType, position, Quaternion.identity);
        }
    }
}
