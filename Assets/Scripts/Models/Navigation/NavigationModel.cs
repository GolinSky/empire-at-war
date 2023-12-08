using System;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Navigation
{
    public interface INavigationModelObserver:IModelObserver
    {
        event Action<Vector2> OnTapPositionChanged;
    }
    
    [CreateAssetMenu(fileName = "NavigationModel", menuName = "Model/NavigationModel")]
    public class NavigationModel:Model, INavigationModelObserver
    {
        public event Action<Vector2> OnTapPositionChanged;

        public Vector2 TapPosition
        {
            set => OnTapPositionChanged?.Invoke(value);
        }
    }
}