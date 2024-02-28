using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using Utilities.ScriptUtils.EditorSerialization;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Map
{
    public interface IMapModelObserver:IModelObserver
    {
        Vector2 Size { get; }
    }
    
    [CreateAssetMenu(fileName = "MapModel", menuName = "Model/MapModel")]
    public class MapModel: Model, IMapModelObserver
    {
        [field:SerializeField] public Vector2 Size { get; private set; }
        [SerializeField] private DictionaryWrapper<PlayerType, Vector3> stationPositionWrapper;

        private Dictionary<PlayerType, Vector3> StationPositions => stationPositionWrapper.Dictionary;

        public Vector3 GetStationPosition(PlayerType opponent)
        {
            return StationPositions[opponent];
        }
    }
}