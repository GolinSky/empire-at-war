using EmpireAtWar.Commands.Game;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Game
{
    public interface ISkirmishGameModelObserver:IModelObserver
    {
        GameTimeMode GameTimeMode { get; }
    }
    
    [CreateAssetMenu(fileName = "SkirmishGameModel", menuName = "Model/SkirmishGameModel")]
    public class SkirmishGameModel: Model, ISkirmishGameModelObserver
    {
        public GameTimeMode GameTimeMode { get; set; }
    }
}