using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.SpaceStation
{
    public interface ISpaceStationModelObserver:IModelObserver
    {
        Vector3 StartPosition { get; }
    }
    [CreateAssetMenu(fileName = "SpaceStationModel", menuName = "Model/SpaceStationModel")]
    public class SpaceStationModel:Model, ISpaceStationModelObserver
    {
        public Vector3 StartPosition { get; set; }
    }
}