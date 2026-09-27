using EmpireAtWar.Models.SkirmishCamera;
using UnityEngine;
using UnityEngine.Serialization;
using Utilities.ScriptUtils.Math;

namespace EmpireAtWar.Services.Camera
{
    [CreateAssetMenu(fileName = nameof(CameraData), menuName = "Data/Camera Data")]
    public class CameraData : ScriptableObject
    {
        [field: FormerlySerializedAs("<MinMoveRangeX>k__BackingField"), SerializeField,
                Tooltip("Offsets added to the map bounds for the camera clamp at the lowest zoom height.")]
        public Vector2Range MinZoomPadding { get; private set; }
        [field: FormerlySerializedAs("<MaxMoveRangeY>k__BackingField"), SerializeField,
                Tooltip("Offsets added to the map bounds for the camera clamp at the highest zoom height.")]
        public Vector2Range MaxZoomPadding { get; private set; }
        [field: SerializeField] public FloatRange ZoomRange { get; private set; }
        [field: SerializeField] public float PanSpeed { get; private set; }
        [field: SerializeField] public float PanAcceleration { get; private set; }
        [field: SerializeField] public float PanDeceleration { get; private set; }
        [field: SerializeField] public float ZoomSpeed { get; private set; }
        [field: SerializeField] public float TweenSpeed { get; private set; }
        [field: SerializeField] public CinematicCameraSettings Cinematic { get; private set; } = new();
    }
}
