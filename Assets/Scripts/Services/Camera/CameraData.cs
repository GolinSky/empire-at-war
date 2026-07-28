using EmpireAtWar.Models.SkirmishCamera;
using UnityEngine;
using Utilities.ScriptUtils.Math;

namespace EmpireAtWar.Services.Camera
{
    [CreateAssetMenu(fileName = nameof(CameraData), menuName = "Data/Camera Data")]
    public class CameraData : ScriptableObject
    {
        [field: SerializeField] public Vector2Range MinMoveRangeX { get; private set; }
        [field: SerializeField] public Vector2Range MaxMoveRangeY { get; private set; }
        [field: SerializeField] public FloatRange ZoomRange { get; private set; }
        [field: SerializeField] public float PanSpeed { get; private set; }
        [field: SerializeField] public float PanAcceleration { get; private set; }
        [field: SerializeField] public float PanDeceleration { get; private set; }
        [field: SerializeField] public float ZoomSpeed { get; private set; }
        [field: SerializeField, Range(1f, 89f)] public float MinPitch { get; private set; }
        [field: SerializeField, Range(1f, 89f)] public float MaxPitch { get; private set; }
        [field: SerializeField] public float OrbitSensitivity { get; private set; }
        [field: SerializeField] public float StepRotationAngle { get; private set; }
        [field: SerializeField] public float TweenSpeed { get; private set; }
    }
}
