using System;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Models.SkirmishCamera
{
    public interface ISkirmishCameraModelObserver: IModelObserver
    {
        event Action<Vector3> OnTranslateDirectionChanged;
        event Action<Vector3> OnPositionChanged;
        Vector3 MapSize { get; }
        
        float MoveSpeed { get; }
        
    }
    [CreateAssetMenu(fileName = "SkirmishCameraModel", menuName = "Model/SkirmishCameraModel")]
    public class SkirmishCameraModel:Model, ISkirmishCameraModelObserver, IInitializable
    {
        public event Action<Vector3> OnTranslateDirectionChanged;
        public event Action<Vector3> OnPositionChanged;
        [field:SerializeField] public Vector3 MapSize { get; private set; }
        [field:SerializeField] public float MoveSpeed { get; private set; }

        public Vector3 TranslateDirection
        {
            set => OnTranslateDirectionChanged?.Invoke(value);
        }
        public Vector3 CameraPosition
        {
            set => OnPositionChanged?.Invoke(value);
        }
        
        public void Initialize()
        {
            
        }
    }
}