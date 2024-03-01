using System;
using EmpireAtWar.Commands.Game;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.SkirmishGame
{
    public interface ISkirmishGameModelObserver:IModelObserver
    {
        event Action<GameTimeMode> OnGameTimeModeChange;
        GameTimeMode GameTimeMode { get; }
    }
    
    [CreateAssetMenu(fileName = "SkirmishGameModel", menuName = "Model/SkirmishGameModel")]
    public class SkirmishGameModel: Model, ISkirmishGameModelObserver
    {
        public event Action<GameTimeMode> OnGameTimeModeChange;

        private GameTimeMode gameTimeMode;
        public GameTimeMode GameTimeMode
        {
            get => gameTimeMode;
            set
            {
                gameTimeMode = value;
                OnGameTimeModeChange?.Invoke(gameTimeMode);
            }
        }
    }
}