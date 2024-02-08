using EmpireAtWar.Models.Skirmish;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Models.Terrain
{
    public interface ITerrainModelObserver:IModel
    {
        
    }
    [CreateAssetMenu(fileName = "TerrainModel", menuName = "Model/TerrainModel")]
    public class TerrainModel:Model, ITerrainModelObserver, IInitializable
    {
        
        [Inject]
        public SkirmishGameData SkirmishGameData { get; }
        public void Initialize()
        {
            
        }
    }
}