using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Terrain
{
    public interface ITerrainModelObserver:IModel
    {
        
    }
    [CreateAssetMenu(fileName = "TerrainModel", menuName = "Model/TerrainModel")]
    public class TerrainModel:Model, ITerrainModelObserver
    {
        
    }
}