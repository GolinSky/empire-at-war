using System;
using EmpireAtWar.Commands.Game;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.SkirmishGame
{
    public interface ICoreGameModelObserver:IModelObserver
    {
        event Action<GameTimeMode> OnGameTimeModeChange;
        GameTimeMode GameTimeMode { get; }
    }
    
    [CreateAssetMenu(fileName = "CoreGameModel", menuName = "Model/Core/CoreGameModel")]
    public class CoreGameModel: Model, ICoreGameModelObserver
    {
        public event Action<GameTimeMode> OnGameTimeModeChange;

        private GameTimeMode _gameTimeMode;

        public GameTimeMode GameTimeMode
        {
            get => _gameTimeMode;
            set
            {
                _gameTimeMode = value;
                OnGameTimeModeChange?.Invoke(_gameTimeMode);
            }
        }
    }
}