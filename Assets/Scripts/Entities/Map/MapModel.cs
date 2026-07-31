using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Mvc;
using UnityEngine;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Entities.Map
{
    public interface IMapModelObserver:IModelObserver
    {
        Vector2Range SizeRange { get; }
        Vector3 GetStationPosition(FactionType factionType);
    }
    
    [CreateAssetMenu(fileName = "MapModel", menuName = "Model/MapModel")]
    public class MapModel: Model, IMapModelObserver
    {
        [SerializeField] private DictionaryWrapper<FactionType, Vector3> stationPositionWrapper;

        [field:SerializeField] public Vector2Range SizeRange { get; private set; }
        private Dictionary<FactionType, Vector3> StationPositions => stationPositionWrapper.Dictionary;

        
        public Vector3 GetStationPosition(FactionType factionType)
        {
            return StationPositions[factionType];
        }
        
    }
}
