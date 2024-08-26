using EmpireAtWar.Models.Factions;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Ship
{
    public class ShipFacadeFactory : PlaceholderFactory<PlayerType,ShipType,Vector3,ShipView> {}
}