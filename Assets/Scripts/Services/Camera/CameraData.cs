using EmpireAtWar.Models.SkirmishCamera;
using UnityEngine;
using Utilities.ScriptUtils.Math;

namespace EmpireAtWar.Services.Camera
{
    [CreateAssetMenu(fileName = nameof(CameraData), menuName = "Data/Camera Data")]
    public class CameraData : ScriptableObject
    {
        [field: SerializeField,
                Tooltip("Offsets added to the map bounds for the camera clamp at the lowest zoom height.")]
        public Vector2Range MinZoomPadding { get; private set; }
        [field: SerializeField,
                Tooltip("Offsets added to the map bounds for the camera clamp at the highest zoom height.")]
        public Vector2Range MaxZoomPadding { get; private set; }
        [field: SerializeField,
                Tooltip("Camera height limits; zoom dollies along the view ray between them.")]
        public FloatRange ZoomRange { get; private set; }
        [field: SerializeField] public float PanSpeed { get; private set; }
        [field: SerializeField] public float PanAcceleration { get; private set; }
        [field: SerializeField] public float PanDeceleration { get; private set; }
        [field: SerializeField,
                Tooltip("Held-key dolly speed along the view ray, in units per second.")]
        public float ZoomSpeed { get; private set; }
        [field: SerializeField,
                Tooltip("Dolly distance along the view ray per mouse-wheel notch (EaW Distance_Per_Mouse_Unit).")]
        public float ZoomStepDistance { get; private set; }
        [field: SerializeField,
                Tooltip("Seconds the camera takes to settle on the zoom target (EaW Distance_Smooth_Time).")]
        public float ZoomSmoothTime { get; private set; }
        [field: SerializeField] public float TweenSpeed { get; private set; }
    }
}
