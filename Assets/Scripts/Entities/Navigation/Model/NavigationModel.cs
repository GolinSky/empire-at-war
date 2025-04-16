using System;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Navigation
{
    public interface INavigationModelObserver:IModelObserver
    {
        event Action<Vector2> OnTapPositionChanged;
        event Action<Vector3> OnAttackPositionChanged;
    }
    
    [CreateAssetMenu(fileName = "NavigationModel", menuName = "Model/NavigationModel")]
    public class NavigationModel:Model, INavigationModelObserver
    {
        public event Action<Vector2> OnTapPositionChanged;
        public event Action<Vector3> OnAttackPositionChanged;

        public Vector2 TapPosition
        {
            set => OnTapPositionChanged?.Invoke(value);
        }
        
        public Vector3 AttackPosition
        {
            set => OnAttackPositionChanged?.Invoke(value);
        }
    }
}