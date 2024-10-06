using System;
using LightWeightFramework.Model;
using Utilities.ScriptUtils.Math;
using UnityEngine;

namespace EmpireAtWar.Models.SkirmishCamera
{
    public interface ICoreCameraModelObserver: IModelObserver
    {
        event Action<Vector3, bool> OnPositionChanged;
        event Action<float> OnFovChanged;

        Vector3 ClampPosition(float heightPercentage, Vector3 position);
        
        float TweenSpeed { get; }
    }

    [CreateAssetMenu(fileName = "CoreCameraModel", menuName = "Model/Core/CoreCameraModel")]
    public class CoreCameraModel:Model, ICoreCameraModelObserver
    {
        public event Action<Vector3> OnTranslateDirectionChanged;
        public event Action<Vector3, bool> OnPositionChanged;
        public event Action<float> OnFovChanged;
        [field:SerializeField] public Vector2Range MinMoveRange { get; private set; }
        [field:SerializeField] public Vector2Range MaxMoveRange { get; private set; }
        [field:SerializeField] public FloatRange ZoomRange { get; private set; }
        [field:SerializeField] public float PanSpeed { get; private set; }
        [field:SerializeField] public float ZoomSpeed { get; private set; }
        public Vector3 ClampPosition(float heightPercentage, Vector3 position)
        {
            float xMin = Mathf.Lerp(MinMoveRange.Min.x, MaxMoveRange.Min.x, heightPercentage);
            float xMax = Mathf.Lerp(MinMoveRange.Max.x, MaxMoveRange.Max.x, heightPercentage);
            float YMin = Mathf.Lerp(MinMoveRange.Min.y, MaxMoveRange.Min.y, heightPercentage);
            float YMax = Mathf.Lerp(MinMoveRange.Max.y, MaxMoveRange.Max.y, heightPercentage);
            position.x = Mathf.Clamp(position.x, xMin, xMax);
            position.z = Mathf.Clamp(position.z, YMin, YMax);
            return position;
        }

        [field:SerializeField] public float TweenSpeed { get; private set; }

        public Vector3 CameraPosition
        {
            set => OnPositionChanged?.Invoke(value, false);
        }
        
        public Vector3 CameraPositionUsingTween
        {
            set => OnPositionChanged?.Invoke(value, true);
        }

        public float FieldOfView
        {
            set => OnFovChanged?.Invoke(value);
        }
    }
}