using EmpireAtWar.Mvc;
using UnityEngine;
using Utilities.ScriptUtils.Math;

namespace EmpireAtWar.Services.Camera
{
    [CreateAssetMenu(fileName = nameof(CameraData), menuName = "Data/Camera Data")]
    public class CameraData : Data
    {
        [field: SerializeField,
                Tooltip("Camera height limits; zoom dollies along the view ray between them.")]
        public FloatRange ZoomRange { get; private set; }
        [field: SerializeField] public float PanSpeed { get; private set; }
        [field: SerializeField] public float PanAcceleration { get; private set; }
        [field: SerializeField] public float PanDeceleration { get; private set; }
        [field: SerializeField,
                Tooltip("Camera height at and above which pan runs at full Pan Speed; below it pan speed shrinks in proportion to height.")]
        public float PanFullSpeedHeight { get; private set; }
        [field: SerializeField,
                Tooltip("Seconds a pan must be held before it starts speeding up.")]
        public float PanBoostDelay { get; private set; }
        [field: SerializeField,
                Tooltip("Seconds the held pan takes to ease from normal speed to the boosted speed.")]
        public float PanBoostRampDuration { get; private set; }
        [field: SerializeField,
                Tooltip("Pan speed multiplier reached at the end of the boost ramp.")]
        public float PanBoostMultiplier { get; private set; }
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
