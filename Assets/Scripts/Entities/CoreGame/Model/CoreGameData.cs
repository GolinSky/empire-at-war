using System;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Models.SkirmishGame
{
    public interface ICoreGameModelObserver:IModelObserver
    {
        event Action<GameTimeMode> OnGameTimeModeChange;
        GameTimeMode GameTimeMode { get; }
        event Action<bool> OnContentVisibilityChanged;
        bool IsContentVisible { get; }
    }
    
    [CreateAssetMenu(fileName = nameof(CoreGameData), menuName = "Data/Core/CoreGameData")]
    public class CoreGameData: Data, IModel, ICoreGameModelObserver
    {
        public event Action<GameTimeMode> OnGameTimeModeChange;
        public event Action<bool> OnContentVisibilityChanged;

        private GameTimeMode _gameTimeMode;
        private bool _isContentVisible;

        public GameTimeMode GameTimeMode
        {
            get => _gameTimeMode;
            set
            {
                _gameTimeMode = value;
                OnGameTimeModeChange?.Invoke(_gameTimeMode);
            }
        }

        public bool IsContentVisible
        {
            get => _isContentVisible;
            set
            {
                if (_isContentVisible == value)
                {
                    return;
                }

                _isContentVisible = value;
                OnContentVisibilityChanged?.Invoke(_isContentVisible);
            }
        }
    }
}