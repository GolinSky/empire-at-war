using System;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.SkirmishCamera
{
    public interface ISkirmishCameraModelObserver: IModelObserver
    {
        event Action<Vector3> OnTranslateDirectionChanged;
        event Action<Vector3> OnPositionChanged;
        Vector3 MapSize { get; }
        
    }
    [CreateAssetMenu(fileName = "SkirmishCameraModel", menuName = "Model/SkirmishCameraModel")]
    public class SkirmishCameraModel:Model, ISkirmishCameraModelObserver
    {
        public event Action<Vector3> OnTranslateDirectionChanged;
        public event Action<Vector3> OnPositionChanged;
        [field:SerializeField] public Vector3 MapSize { get; private set; }

        public Vector3 TranslateDirection
        {
            set => OnTranslateDirectionChanged?.Invoke(value);
        }
        public Vector3 CameraPosition
        {
            set => OnPositionChanged?.Invoke(value);
        }
        
    }
}