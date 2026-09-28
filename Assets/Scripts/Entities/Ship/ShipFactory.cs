using EmpireAtWar.Models.Factions;
using UnityEngine;
using EmpireAtWar.Models.Players;
using Zenject;

namespace EmpireAtWar.Ship
{
    public class ShipFactory : PlaceholderFactory<PlayerId,ShipType,Vector3,Ship> {}
}
