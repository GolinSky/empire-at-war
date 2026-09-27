using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    public interface IMapModelObserver : IModelObserver
    {
        Vector2Range SizeRange { get; }
        Vector3 GetStationPosition(FactionType factionType);
    }
}
