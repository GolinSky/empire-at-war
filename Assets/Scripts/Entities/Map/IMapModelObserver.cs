using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    public interface IMapModelObserver : IModelObserver
    {
        Vector2Range SizeRange { get; }

        Vector3 GetStationPosition(PlayerId owner);
    }
}
