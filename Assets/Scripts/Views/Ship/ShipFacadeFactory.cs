using EmpireAtWar.Models.Factions;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Views.Ship
{
    public class ShipFacadeFactory : PlaceholderFactory<PlayerType,ShipType,Vector3,ShipView> {}
}