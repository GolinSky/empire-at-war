using System;
using EmpireAtWar.Models.SkirmishCamera;
using LightWeightFramework.Model;
using UnityEngine;
using Utilities.ScriptUtils.Math;

namespace EmpireAtWar.Entities.SkirmishCamera
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
        [field:SerializeField] public Vector2Range MinMoveRangeX { get; private set; }
        [field:SerializeField] public Vector2Range MaxMoveRangeY { get; private set; }
        [field:SerializeField] public FloatRange ZoomRange { get; private set; }
        [field:SerializeField] public float PanSpeed { get; private set; }
        [field:SerializeField] public float ZoomSpeed { get; private set; }
        public Vector3 ClampPosition(float heightPercentage, Vector3 position)
        {
            float xMin = Mathf.Lerp(MinMoveRangeX.Min.x, MaxMoveRangeY.Min.x, heightPercentage);
            float xMax = Mathf.Lerp(MinMoveRangeX.Max.x, MaxMoveRangeY.Max.x, heightPercentage);
            float yMin = Mathf.Lerp(MinMoveRangeX.Min.y, MaxMoveRangeY.Min.y, heightPercentage);
            float yMax = Mathf.Lerp(MinMoveRangeX.Max.y, MaxMoveRangeY.Max.y, heightPercentage);
            position.x = Mathf.Clamp(position.x, xMin, xMax);
            position.z = Mathf.Clamp(position.z, yMin, yMax);
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