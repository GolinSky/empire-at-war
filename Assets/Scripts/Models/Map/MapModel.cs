using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.SkirmishCamera;
using Utilities.ScriptUtils.EditorSerialization;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Map
{
    public interface IMapModelObserver:IModelObserver
    {
        Vector2Range SizeRange { get; }
        Vector3 GetStationPosition(PlayerType opponent);
    }
    
    [CreateAssetMenu(fileName = "MapModel", menuName = "Model/MapModel")]
    public class MapModel: Model, IMapModelObserver
    {
        [SerializeField] private DictionaryWrapper<PlayerType, Vector3> stationPositionWrapper;

        [field:SerializeField] public Vector2Range SizeRange { get; private set; }
        private Dictionary<PlayerType, Vector3> StationPositions => stationPositionWrapper.Dictionary;

        public Vector3 GetStationPosition(PlayerType opponent)
        {
            return StationPositions[opponent];
        }
        
    }
}