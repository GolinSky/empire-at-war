using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Models.ShipUi
{
    public interface IShipIconProvider
    {
        Sprite GetShipIcon(ShipType shipType);

        Sprite GetSquadronIcon(SquadronType squadronType);
    }
}
