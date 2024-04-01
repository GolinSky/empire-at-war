using System;
using LightWeightFramework.Model;
using Utilities.ScriptUtils.Math;
using UnityEngine;

namespace EmpireAtWar.Models.SkirmishCamera
{
    public interface ICoreCameraModelObserver: IModelObserver
    {
        event Action<Vector3> OnTranslateDirectionChanged;
        event Action<Vector3> OnPositionChanged;
        event Action<float> OnFovChanged;
        
        Vector2Range MoveRange { get;}
        
    }

    [CreateAssetMenu(fileName = "CoreCameraModel", menuName = "Model/Core/CoreCameraModel")]
    public class CoreCameraModel:Model, ICoreCameraModelObserver
    {
        public event Action<Vector3> OnTranslateDirectionChanged;
        public event Action<Vector3> OnPositionChanged;
        public event Action<float> OnFovChanged;
        [field:SerializeField] public Vector2Range MoveRange { get; private set; }
        [field:SerializeField] public FloatRange ZoomRange { get; private set; }
        [field:SerializeField] public float PanSpeed { get; private set; }
        
        [field:SerializeField] public float ZoomSpeed { get; private set; }
        public Vector3 TranslateDirection
        {
            set => OnTranslateDirectionChanged?.Invoke(value);
        }
        public Vector3 CameraPosition
        {
            set => OnPositionChanged?.Invoke(value);
        }

        public float FieldOfView
        {
            set => OnFovChanged?.Invoke(value);
        }
    }
}