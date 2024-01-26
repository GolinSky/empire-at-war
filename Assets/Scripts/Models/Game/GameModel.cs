using EmpireAtWar.Commands.Game;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Game
{
    public interface IGameModelObserver:IModelObserver
    {
        GameTimeMode GameTimeMode { get; }
    }
    
    [CreateAssetMenu(fileName = "GameModel", menuName = "Model/GameModel")]
    public class GameModel:Model, IGameModelObserver
    {
        public GameTimeMode GameTimeMode { get; set; }
    }
}